using System;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using Util;

[ExecuteAlways, RequireComponent(typeof(MeshRenderer), typeof(MeshFilter))]
public class TubeMesh : MonoBehaviour
{
    private delegate void GizmoDrawCall();

    [Header("Properties")]
    [SerializeField] public Vector3[] points;
    [SerializeField] public int sides;
    [SerializeField] public float radius;
    [SerializeField] public bool worldSpace = true;

    private void Awake()
    {
        if (worldSpace)
        {
            transform.position = Vector3.zero;
            transform.rotation = Quaternion.identity;
        }
    }
    private void LateUpdate()
    {
        if(!Application.isPlaying) UpdateMesh();

        if (worldSpace)
        {
            transform.position = Vector3.zero;
            transform.rotation = Quaternion.identity;
        }
    }

    public void UpdateMesh()
    {
        if (sides < 3)
        {
            Debug.LogWarning("Tried to set tube mesh's side count below 3");
            return;
        }
        
        var filter = GetComponent<MeshFilter>();
        if (points.Length < 2 || points[0] == points[1])
        {
            filter.mesh = null;
            return;
        }

        gizmoDrawCalls.Clear();
        
        // Build vertices
        Vector3[] vertices = new Vector3[points.Length * sides + 2];
        
        // place the points along the tube
        float vertexSeparation = 2 * Mathf.PI / sides;
        for (int p = 0; p < points.Length; p++)
        {
            Vector3 pos = points[p];
            
            // Weighs the direction of longer segments more strongly, so only the topology of short sections should get too messed up.
            Vector3 forward = Vector3.zero;
            if (p + 1 < points.Length) forward += points[p + 1] - pos;
            if (p - 1 >= 0) forward += pos - points[p - 1];
            
            if (forward == Vector3.zero) forward = Vector3.forward;
            else forward.Normalize();
            
            Quaternion lookRot = Quaternion.LookRotation(forward);
            Vector3 xBasis = lookRot * Vector3.right * radius;
            Vector3 yBasis = lookRot * Vector3.up * radius;

            //gizmoDrawCalls.Add(() =>
            //{
            //    ArrowGizmo.Draw(pos + transform.position, forward, Color.blue);
            //    ArrowGizmo.Draw(pos + transform.position, xBasis / radius, Color.red);
            //    ArrowGizmo.Draw(pos + transform.position, yBasis / radius, Color.green);
            //});
            
            for (int v = 0; v < sides; v++)
            {
                int vertIdx = p * sides + v;
                
                float angle = v * vertexSeparation;
                Vector3 offset = xBasis * Mathf.Cos(angle) + yBasis * Mathf.Sin(angle);
                
                vertices[vertIdx] = points[p] + offset;
            }
        }

        // Set up the midpoints of the end caps
        vertices[^2] = points[0];
        vertices[^1] = points[^1];
        
        // Stitch together vertices
        int[] triangles = new int[sides * 2 * (points.Length - 1) * 3 + sides * 3 * 2];
        
        // Connect all the sides of each segment
        for (int segment = 0; segment < points.Length - 1; segment++)
        {
            int segmentTriOffset = sides * 2 * segment * 3;
            
            int leftOffset = segment * sides;
            int rightOffset = (segment + 1) * sides;
            for (int side = 0; side < sides; side++)
            {
                int triOffset = segmentTriOffset + side * 2 * 3;
                
                int bottomIdx = side;
                int topIdx = (side + 1) % sides;

                triangles[triOffset + 0] = leftOffset + topIdx;
                triangles[triOffset + 1] = rightOffset + topIdx;
                triangles[triOffset + 2] = leftOffset + bottomIdx;
                triangles[triOffset + 3] = rightOffset + bottomIdx;
                triangles[triOffset + 4] = leftOffset + bottomIdx;
                triangles[triOffset + 5] = rightOffset + topIdx;
            }
        }
        
        // Stitch together the end-caps
        int frontCenter = vertices.Length - 2;
        int backCenter = vertices.Length - 1;
        for (int face = 0; face < sides; face++)
        {
            int frontVertOffset = face;
            int backVertOffset = vertices.Length - (face + 1) - 2;
            int toNextVert = face == sides - 1 ? (1 - sides) : 1;

            int frontTriOffset = triangles.Length - ((face + 1) * 3 + sides * 3);
            int backTriOffset = triangles.Length - ((face + 1) * 3);
            
            triangles[frontTriOffset + 0] = frontVertOffset;
            triangles[frontTriOffset + 1] = frontCenter;
            triangles[frontTriOffset + 2] = frontVertOffset + toNextVert;
            
            triangles[backTriOffset + 0] = backVertOffset;
            triangles[backTriOffset + 1] = backCenter;
            triangles[backTriOffset + 2] = backVertOffset - toNextVert;
        }
        
        Mesh newMesh = new();
        newMesh.vertices = vertices;
        newMesh.triangles = triangles;
        filter.mesh = newMesh;
    }

    private List<GizmoDrawCall> gizmoDrawCalls = new();

    private void OnDrawGizmos()
    {
        foreach (var g in gizmoDrawCalls) g();
    }
}
