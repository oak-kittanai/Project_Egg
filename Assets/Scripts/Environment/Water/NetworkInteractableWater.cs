using System.Collections.Generic;
using UnityEngine;
using Fusion;

[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))]
[RequireComponent(typeof(EdgeCollider2D))]
public class NetworkInteractableWater : NetworkBehaviour
{
    [Header("Dimensions")]
    public int vertexCount = 20;
    public Vector2 size = new Vector2(10f, 4f);

    [Header("Visual Layers & Parallax")]
    [Tooltip("ระยะที่ต้องการยืดภาพน้ำออกไปด้านข้างซ้าย-ขวา สำหรับทำ Parallax")]
    public float extendX = 5f;
    [Tooltip("ความลึกของชั้นน้ำตื้น")]
    public float shallowDepth = 1.5f;

    [Space(10)]
    public Color surfaceColor = new Color(0.5f, 0.8f, 1f, 1f); // สีผิวน้ำ
    public Color shallowColor = new Color(0.1f, 0.4f, 0.8f, 0.9f); // สีน้ำตื้น
    public Color deepColor = new Color(0f, 0.1f, 0.4f, 1f); // สีน้ำลึก

    [Header("Material")]
    [Tooltip("แนะนำให้ใช้ Material เช่น Sprites-Default เพื่อให้รองรับ Vertex Colors")]
    public Material material;
    public Color gizmoColor = Color.cyan;

    [Header("Physics Settings")]
    public float springStiffness = 0.1f;
    public float dampening = 0.03f;
    public float spread = 0.006f;
    public float collisionRadius = 1f;
    public float forceMultiplier = 1f;
    public float maxVelocity = 5f;

    [HideInInspector]
    public List<WaterPoint> waterPoints = new List<WaterPoint>();

    private MeshFilter meshFilter;
    private MeshRenderer meshRenderer;
    private EdgeCollider2D edgeCollider;
    private Mesh mesh;
    private Vector3[] vertices;
    private int[] topVertexIndices;

    // จุดยืดซ้ายสุด-ขวาสุด สำหรับให้ขอบน้ำขยับตามคลื่นด้วย
    private int leftExtIndex;
    private int rightExtIndex;

    private const int NUM_Y_VERTICES = 3; // เปลี่ยนเป็น 3 ชั้น (ผิวน้ำ, ตื้น, ลึก)

    [System.Serializable]
    public class WaterPoint
    {
        public float velocity;
        public float height;
        public float targetHeight;
    }

    private void Awake()
    {
        meshFilter = GetComponent<MeshFilter>();
        meshRenderer = GetComponent<MeshRenderer>();
        edgeCollider = GetComponent<EdgeCollider2D>();

        GenerateMesh();
        CreateWaterPoints();

        if (edgeCollider) edgeCollider.isTrigger = true;
    }

    [Rpc(RpcSources.All, RpcTargets.All)]
    public void RPC_Splash(Vector3 position, float velocity)
    {
        ApplySplashPhysics(position, velocity);
    }

    private void ApplySplashPhysics(Vector3 position, float velocity)
    {
        Vector3 localPos = transform.InverseTransformPoint(position);

        for (int i = 0; i < waterPoints.Count; i++)
        {
            Vector3 vertPos = vertices[topVertexIndices[i]];
            float distance = Mathf.Abs(localPos.x - vertPos.x);

            if (distance < collisionRadius)
            {
                waterPoints[i].velocity += velocity * forceMultiplier;
            }
        }
    }

    private void FixedUpdate()
    {
        for (int i = 0; i < waterPoints.Count; i++)
        {
            WaterPoint point = waterPoints[i];
            float x = point.height - point.targetHeight;
            float acceleration = -springStiffness * x - dampening * point.velocity;
            point.velocity += acceleration;
            point.height += point.velocity;

            if (Mathf.Abs(point.velocity) > maxVelocity)
                point.velocity = Mathf.Sign(point.velocity) * maxVelocity;
        }

        for (int j = 0; j < 8; j++)
        {
            for (int i = 0; i < waterPoints.Count; i++)
            {
                if (i > 0)
                {
                    float leftDelta = spread * (waterPoints[i].height - waterPoints[i - 1].height);
                    waterPoints[i - 1].velocity += leftDelta;
                }
                if (i < waterPoints.Count - 1)
                {
                    float rightDelta = spread * (waterPoints[i].height - waterPoints[i + 1].height);
                    waterPoints[i + 1].velocity += rightDelta;
                }
            }
        }

        UpdateVertices();
    }

    public void GenerateMesh()
    {
        mesh = new Mesh();

        // จำนวน X ทั้งหมด = จุดฟิสิกส์ + ขอบซ้าย 1 + ขอบขวา 1
        int actualXCount = vertexCount + 2;

        vertices = new Vector3[actualXCount * NUM_Y_VERTICES];
        Color[] colors = new Color[vertices.Length];
        Vector2[] uvs = new Vector2[vertices.Length];
        int[] triangles = new int[(actualXCount - 1) * (NUM_Y_VERTICES - 1) * 6];

        topVertexIndices = new int[vertexCount];

        int vIndex = 0;

        // วนลูปสร้าง 3 ชั้น: y=0(ลึกสุด), y=1(น้ำตื้น), y=2(ผิวน้ำ)
        for (int y = 0; y < NUM_Y_VERTICES; y++)
        {
            float yPos = 0;
            Color rowColor = Color.white;

            if (y == 0) { yPos = -size.y; rowColor = deepColor; }
            else if (y == 1) { yPos = -shallowDepth; rowColor = shallowColor; }
            else if (y == 2) { yPos = 0; rowColor = surfaceColor; }

            for (int x = 0; x < actualXCount; x++)
            {
                float xPos = 0;

                // สร้างส่วนยืดซ้าย-ขวา
                if (x == 0) xPos = -extendX;
                else if (x == actualXCount - 1) xPos = size.x + extendX;
                else xPos = ((float)(x - 1) / (vertexCount - 1)) * size.x;

                vertices[vIndex] = new Vector3(xPos, yPos, 0);
                colors[vIndex] = rowColor; // ใส่สีแต่ละชั้น
                uvs[vIndex] = new Vector2((float)x / (actualXCount - 1), (float)y / (NUM_Y_VERTICES - 1));

                // เก็บ Index ของผิวน้ำเพื่อเอาไปขยับขึ้นลง (คลื่น)
                if (y == 2)
                {
                    if (x > 0 && x < actualXCount - 1) topVertexIndices[x - 1] = vIndex;
                    else if (x == 0) leftExtIndex = vIndex;
                    else if (x == actualXCount - 1) rightExtIndex = vIndex;
                }
                vIndex++;
            }
        }

        int tIndex = 0;
        for (int y = 0; y < NUM_Y_VERTICES - 1; y++)
        {
            for (int x = 0; x < actualXCount - 1; x++)
            {
                int bottomLeft = y * actualXCount + x;
                int bottomRight = bottomLeft + 1;
                int topLeft = bottomLeft + actualXCount;
                int topRight = topLeft + 1;

                triangles[tIndex++] = bottomLeft;
                triangles[tIndex++] = topLeft;
                triangles[tIndex++] = bottomRight;

                triangles[tIndex++] = bottomRight;
                triangles[tIndex++] = topLeft;
                triangles[tIndex++] = topRight;
            }
        }

        mesh.vertices = vertices;
        mesh.colors = colors; // ส่งสีเข้า Mesh
        mesh.triangles = triangles;
        mesh.uv = uvs;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        if (meshFilter == null) meshFilter = GetComponent<MeshFilter>();
        if (meshRenderer == null) meshRenderer = GetComponent<MeshRenderer>();

        meshFilter.mesh = mesh;
        if (material != null) meshRenderer.material = material;
    }

    public void ResetEdgeCollider()
    {
        if (edgeCollider == null) edgeCollider = GetComponent<EdgeCollider2D>();

        Vector2[] points = new Vector2[2];
        points[0] = Vector2.zero;
        points[1] = new Vector2(size.x, 0); // Collider ขยับแค่ในไซส์จริง ไม่รวมระยะยืด Parallax

        edgeCollider.points = points;
        edgeCollider.offset = Vector2.zero;
    }

    public void CreateWaterPoints()
    {
        waterPoints.Clear();
        for (int i = 0; i < vertexCount; i++)
        {
            WaterPoint p = new WaterPoint();
            if (topVertexIndices != null && topVertexIndices.Length > i)
            {
                p.height = vertices[topVertexIndices[i]].y;
                p.targetHeight = vertices[topVertexIndices[i]].y;
                waterPoints.Add(p);
            }
        }
    }

    private void UpdateVertices()
    {
        if (vertices == null || waterPoints == null) return;

        // อัปเดตคลื่นเฉพาะโซนหลัก
        for (int i = 0; i < waterPoints.Count; i++)
        {
            if (i < topVertexIndices.Length)
                vertices[topVertexIndices[i]].y = waterPoints[i].height;
        }

        // ทำให้ระยะที่ยืดออกไป (Parallax) ขยับเนียนไปกับคลื่นขอบสุด
        if (waterPoints.Count > 0)
        {
            vertices[leftExtIndex].y = waterPoints[0].height;
            vertices[rightExtIndex].y = waterPoints[waterPoints.Count - 1].height;
        }

        mesh.vertices = vertices;
    }
}