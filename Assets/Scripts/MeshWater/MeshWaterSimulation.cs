using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(MeshFilter))]
public class MeshWaterSimulation : MonoBehaviour
{
    [Header("Spring‑Damper Settings")]
    public float springConstant = 50f;
    public float damping = 5f;
    public float forceMultiplier = 1f;

    private Mesh mesh;
    private Vector3[] baseVerts;
    private float[] heights, velocities;

    // Updated each frame by GazingBoxController (e.g., boxRigidbody.velocity or manual tracking)
    public Vector3 boxAcceleration;

    void Start()
    {
        mesh = GetComponent<MeshFilter>().mesh;
        baseVerts = mesh.vertices;
        int count = baseVerts.Length;
        heights = new float[count];
        velocities = new float[count];
        for (int i = 0; i < count; i++) heights[i] = 0f;
    }

    void Update()
    {
        float dt = Time.deltaTime;
        float extF = boxAcceleration.y * forceMultiplier;

        for (int i = 0; i < heights.Length; i++)
        {
            float disp = heights[i];
            float accel = -springConstant * disp - damping * velocities[i] + extF;
            velocities[i] += accel * dt;
            heights[i] += velocities[i] * dt;
        }
        ApplyMesh();
    }

    void ApplyMesh()
    {
        Vector3[] verts = new Vector3[baseVerts.Length];
        for (int i = 0; i < verts.Length; i++)
        {
            verts[i] = baseVerts[i];
            verts[i].y += heights[i];
        }
        mesh.vertices = verts;
        mesh.RecalculateNormals();
    }
}
