using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class BoatMovement : MonoBehaviour
{
    public float forwardForce = 20f;
    public float turnTorque = 10f;
    private Rigidbody rb;

    void Start() => rb = GetComponent<Rigidbody>();

    void FixedUpdate()
    {
        float move = Input.GetAxis("Vertical");   // W/S or Up/Down
        float turn = Input.GetAxis("Horizontal"); // A/D or Left/Right

        rb.AddForce(transform.forward * move * forwardForce, ForceMode.Force);
        rb.AddTorque(Vector3.up * turn * turnTorque, ForceMode.Force);
    }
}
