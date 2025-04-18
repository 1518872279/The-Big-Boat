using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MeshGazingBox : MonoBehaviour
{
    [Header("Control Settings")]
    public float rotationSensitivity = 0.1f;
    public float maxTilt = 30f;
    public float returnSpeed = 2f;

    private Vector3 targetOffset = Vector3.zero;
    private Quaternion neutralRot;
    private Vector3 prevMousePos;
    private bool dragging = false;

    void Start()
    {
        neutralRot = transform.rotation;
    }

    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            dragging = true;
            prevMousePos = Input.mousePosition;
        }
        if (Input.GetMouseButtonUp(0)) dragging = false;

        if (dragging)
        {
            Vector3 delta = (Vector3)Input.mousePosition - prevMousePos;
            targetOffset.x += delta.y * rotationSensitivity;
            targetOffset.z -= delta.x * rotationSensitivity;
            targetOffset.x = Mathf.Clamp(targetOffset.x, -maxTilt, maxTilt);
            targetOffset.z = Mathf.Clamp(targetOffset.z, -maxTilt, maxTilt);
            prevMousePos = Input.mousePosition;
        }
        else
        {
            targetOffset = Vector3.Lerp(targetOffset, Vector3.zero, Time.deltaTime * returnSpeed);
        }

        Quaternion offsetRot = Quaternion.Euler(targetOffset);
        transform.rotation = Quaternion.Slerp(transform.rotation, neutralRot * offsetRot, Time.deltaTime * 5f);
    }
}
