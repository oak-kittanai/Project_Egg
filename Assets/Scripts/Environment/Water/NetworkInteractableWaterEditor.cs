using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(NetworkInteractableWater))]
public class NetworkInteractableWaterEditor : Editor
{
    public override void OnInspectorGUI()
    {
        EditorGUI.BeginChangeCheck();

        DrawDefaultInspector();

        NetworkInteractableWater water = (NetworkInteractableWater)target;

        GUILayout.Space(10);

        if (GUILayout.Button("Generate Mesh"))
        {
            water.GenerateMesh();
            water.ResetEdgeCollider();
            water.CreateWaterPoints();
        }

        if (GUILayout.Button("Snap Edge Collider"))
        {
            water.ResetEdgeCollider();
        }

        if (EditorGUI.EndChangeCheck())
        {
            water.GenerateMesh();
        }
    }

    protected virtual void OnSceneGUI()
    {
        NetworkInteractableWater water = (NetworkInteractableWater)target;

        Handles.color = Color.white;

        Vector3 center = water.transform.position;
        Vector3 p1 = water.transform.TransformPoint(new Vector3(water.size.x, 0, 0));
        Vector3 p2 = water.transform.TransformPoint(new Vector3(0, -water.size.y, 0));
        Vector3 p3 = water.transform.TransformPoint(new Vector3(water.size.x, -water.size.y, 0));

        Handles.color = new Color(1, 0.5f, 0);
        Vector3[] corners = new Vector3[] {
            water.transform.position, p1, p3, p2, water.transform.position
        };
        Handles.DrawPolyLine(corners);

        EditorGUI.BeginChangeCheck();

        Vector3 newP1 = Handles.FreeMoveHandle(p1, 0.2f, Vector3.zero, Handles.CubeHandleCap);

        Vector3 newP2 = Handles.FreeMoveHandle(p2, 0.2f, Vector3.zero, Handles.CubeHandleCap);

        Vector3 newP3 = Handles.FreeMoveHandle(p3, 0.2f, Vector3.zero, Handles.CubeHandleCap);

        if (EditorGUI.EndChangeCheck())
        {
            Vector3 localP1 = water.transform.InverseTransformPoint(newP1);
            Vector3 localP2 = water.transform.InverseTransformPoint(newP2);
            Vector3 localP3 = water.transform.InverseTransformPoint(newP3);

            if (newP1 != p1) water.size.x = Mathf.Max(0.1f, localP1.x);
            if (newP2 != p2) water.size.y = Mathf.Max(0.1f, -localP2.y);
            if (newP3 != p3)
            {
                water.size.x = Mathf.Max(0.1f, localP3.x);
                water.size.y = Mathf.Max(0.1f, -localP3.y);
            }

            water.GenerateMesh();
            water.ResetEdgeCollider();
        }
    }
}