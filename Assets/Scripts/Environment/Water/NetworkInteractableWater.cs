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
    public Vector2 size = new Vector2(10f, 4f); // X = Width, Y = Height

    [Header("Visuals")]
    public Material material;
    public Color gizmoColor = Color.cyan;

    [Header("Physics Settings")]
    public float springStiffness = 0.1f;
    public float dampening = 0.03f;
    public float spread = 0.006f;
    public float collisionRadius = 1f;
    public float forceMultiplier = 1f;
    public float maxVelocity = 5f;

    // Simulation Data
    [HideInInspector]
    public List<WaterPoint> waterPoints = new List<WaterPoint>();

    // Components
    private MeshFilter meshFilter;
    private MeshRenderer meshRenderer;
    private EdgeCollider2D edgeCollider;
    private Mesh mesh;
    private Vector3[] vertices;
    private int[] topVertexIndices;
    private const int NUM_Y_VERTICES = 2; // กลับมาใช้ 2 ชั้น (บน-ล่าง)

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

        // Ensure mesh exists on start
        GenerateMesh();
        CreateWaterPoints();

        if (edgeCollider) edgeCollider.isTrigger = true;
    }

    // --- NETWORK LOGIC ---
    [Rpc(RpcSources.All, RpcTargets.All)]
    public void RPC_Splash(Vector3 position, float velocity)
    {
        ApplySplashPhysics(position, velocity);
    }

    private void ApplySplashPhysics(Vector3 position, float velocity)
    {
        // Convert world position to local
        Vector3 localPos = transform.InverseTransformPoint(position);

        for (int i = 0; i < waterPoints.Count; i++)
        {
            Vector3 vertPos = vertices[topVertexIndices[i]];
            // Simple distance check
            float distance = Mathf.Abs(localPos.x - vertPos.x);

            if (distance < collisionRadius)
            {
                waterPoints[i].velocity += velocity * forceMultiplier;
            }
        }
    }

    // --- PHYSICS LOOP ---
    private void FixedUpdate()
    {
        // 1. Update Springs
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

        // 2. Wave Propagation
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

        // 3. Update Mesh
        UpdateVertices();
    }

    // --- MESH GENERATION ---
    public void GenerateMesh()
    {
        mesh = new Mesh();
        vertices = new Vector3[vertexCount * NUM_Y_VERTICES];
        topVertexIndices = new int[vertexCount];
        Vector2[] uvs = new Vector2[vertices.Length];
        int[] triangles = new int[(vertexCount - 1) * 6];

        int vIndex = 0;
        for (int y = 0; y < NUM_Y_VERTICES; y++)
        {
            for (int x = 0; x < vertexCount; x++)
            {
                // Calculate position based on the Size variable
                float xCoordinate = ((float)x / (vertexCount - 1)) * size.x;
                float yCoordinate = (y == 0) ? -size.y : 0f;

                vertices[vIndex] = new Vector3(xCoordinate, yCoordinate, 0);
                uvs[vIndex] = new Vector2((float)x / (vertexCount - 1), y);

                if (y == 1) topVertexIndices[x] = vIndex;
                vIndex++;
            }
        }

        int tIndex = 0;
        for (int x = 0; x < vertexCount - 1; x++)
        {
            int bottomLeft = x;
            int bottomRight = x + 1;
            int topLeft = x + vertexCount;
            int topRight = x + 1 + vertexCount;

            // Triangle 1
            triangles[tIndex++] = bottomLeft;
            triangles[tIndex++] = topLeft;
            triangles[tIndex++] = bottomRight;

            // Triangle 2
            triangles[tIndex++] = bottomRight;
            triangles[tIndex++] = topLeft;
            triangles[tIndex++] = topRight;
        }

        mesh.vertices = vertices;
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
        points[1] = new Vector2(size.x, 0); // Use dynamic size.x

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

        for (int i = 0; i < waterPoints.Count; i++)
        {
            if (i < topVertexIndices.Length)
                vertices[topVertexIndices[i]].y = waterPoints[i].height;
        }
        mesh.vertices = vertices;
    }
}