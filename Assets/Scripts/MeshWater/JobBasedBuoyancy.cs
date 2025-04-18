using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Jobs;
using Unity.Collections;
using Unity.Burst;

public class JobBasedBuoyancy : MonoBehaviour
{
    public Transform[] samplePoints;
    public float waterLevel = 0f;
    public float buoyancyLift = 2f;
    public float waterDrag = 0.5f;
    public Rigidbody boatRb;

    private NativeArray<Vector3> positions;
    private NativeArray<float> disps;

    void Start()
    {
        int n = samplePoints.Length;
        positions = new NativeArray<Vector3>(n, Allocator.Persistent);
        disps = new NativeArray<float>(n, Allocator.Persistent);
    }

    void FixedUpdate()
    {
        for (int i = 0; i < positions.Length; i++)
            positions[i] = samplePoints[i].position;

        var job = new BuoyancyJob
        {
            positions = positions,
            waterLevel = waterLevel,
            disps = disps
        };
        var handle = job.Schedule(positions.Length, 16);
        handle.Complete();

        for (int i = 0; i < disps.Length; i++)
        {
            float d = disps[i];
            if (d > 0f)
            {
                Vector3 lift = Vector3.up * buoyancyLift * d;
                boatRb.AddForceAtPosition(lift, positions[i], ForceMode.Force);
                boatRb.AddForce(-boatRb.velocity * waterDrag * d, ForceMode.Force);
            }
        }
    }

    void OnDestroy()
    {
        positions.Dispose();
        disps.Dispose();
    }

    [BurstCompile]
    struct BuoyancyJob : IJobParallelFor
    {
        [ReadOnly] public NativeArray<Vector3> positions;
        public float waterLevel;
        public NativeArray<float> disps;

        public void Execute(int i)
        {
            float depth = waterLevel - positions[i].y;
            disps[i] = Mathf.Max(depth, 0f);
        }
    }
}
