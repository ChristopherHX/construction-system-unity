using System;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class LockRotation : MonoBehaviour
{
    private Rigidbody _rigidbody;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        _rigidbody = GetComponent<Rigidbody>();
    }

    // // Update is called once per frame
    // void FixedUpdate()
    // {
    //     _rigidbody.angularVelocity = new Vector3(Mathf.Clamp(_rigidbody.angularVelocity.x, -90, 90), _rigidbody.angularVelocity.y, Mathf.Clamp(_rigidbody.angularVelocity.z, -90, 90));

    //     var angles = _rigidbody.rotation.eulerAngles;
    //     angles.y = 0;
    //     angles.x = Mathf.Abs(angles.x) < 1f ? 0 : Mathf.Sign(angles.x) * 10;
    //     angles.z = Mathf.Abs(angles.z) < 1f ? 0 : Mathf.Sign(angles.z) * 10;
    //     // Torque ist Drehmoment
    //     _rigidbody.AddTorque(-angles);
    // }

    // Based on https://copilot.microsoft.com/shares/e6rTxAvrztmZKLmBEPm43

    public float torqueStrength = 10f;
    public float damping = 2f;

    void FixedUpdate()
    {
        var rb = _rigidbody;
        // // 1. Direction to target
        // Vector3 toTarget = (target.position - rb.position).normalized;

        // 2. Desired rotation
        Quaternion desiredRot = Quaternion.identity;

        // 3. Rotation difference
        Quaternion delta = desiredRot * Quaternion.Inverse(rb.rotation);

        // Convert quaternion to axis-angle
        delta.ToAngleAxis(out float angle, out Vector3 axis);

        // Fix weird cases
        if (float.IsNaN(axis.x)) return;

        // Convert angle to radians and clamp
        angle = Mathf.DeltaAngle(0, angle);

        // 4. Apply torque
        Vector3 acc = axis.normalized * angle * torqueStrength;

        // // Add damping to prevent overshoot
        // torque -= rb.angularVelocity * damping;

        // ❗ Only rotate around X and Z
        acc.y = 0f;

        // Add damping
        acc -= new Vector3(rb.angularVelocity.x, 0f, rb.angularVelocity.z) * damping;

        // rb.AddTorque(acc, ForceMode.Acceleration);

        // Calculate equivalent torque
        Quaternion invRot = Quaternion.Inverse(rb.inertiaTensorRotation);
        Vector3 localAcc = invRot * acc;

        Vector3 localTorque = Vector3.Scale(rb.inertiaTensor, localAcc);
        Vector3 torque = rb.inertiaTensorRotation * localTorque;

        // Debug.Log("Equivalent torque: " + torque);
        rb.AddTorque(torque, ForceMode.Force);
    }
}
