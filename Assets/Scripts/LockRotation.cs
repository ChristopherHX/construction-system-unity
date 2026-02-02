using System;
using UnityEditor;
using UnityEngine;

// [ExecuteInEditMode]
[RequireComponent(typeof(Rigidbody))]
public class LockRotation : MonoBehaviour
{
    private Rigidbody _rigidbody;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        _rigidbody = GetComponent<Rigidbody>();

        EditorApplication.pauseStateChanged += OnPauseStateChanged;
    }

    private void OnPauseStateChanged(PauseState state)
    {
        if(state == PauseState.Paused)
        {
            modify = false;
            EditorApplication.update += FixedUpdate;
        }
        else
        {
            EditorApplication.update -= FixedUpdate;
            modify = true;
        }
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
    public float damping = 50f;

    public string rawDesiredRotation;
    public Vector3 desiredRotation;
    public Vector3 currentRotation;
    public string rawRotation;
    

    private bool modify = true;

    private Quaternion RemoveRollPitch(Quaternion quat)
    {
        return Quaternion.FromToRotation(quat * Vector3.up, Vector3.up);
    }

    void FixedUpdate()
    {
        var rb = _rigidbody;
        // // 1. Direction to target
        // Vector3 toTarget = (target.position - rb.position).normalized;

        // 2. Desired rotation
        // Quaternion desiredRot = Quaternion.identity * Quaternion.Euler(90, 90, 0);
        // Vector3 forward = transform.forward;
        // forward.y = 0f; // remove vertical component
        // Quaternion desiredRot = Quaternion.LookRotation(forward);
        // Quaternion q = transform.rotation;

        // // Extract yaw (Y rotation) from the quaternion
        // float yaw = Mathf.Atan2(
        //     2f * (q.w * q.y + q.x * q.z),
        //     1f - 2f * (q.y * q.y + q.x * q.x)
        // ) * Mathf.Rad2Deg;

        // // Rebuild rotation with only Y
        // Quaternion desiredRot = Quaternion.Euler(0f, yaw, 0f);

        // Quaternion q = transform.rotation;

        // // Extract yaw from quaternion
        // float yaw = Mathf.Atan2(
        //     2f * (q.w * q.y + q.x * q.z),
        //     1f - 2f * (q.y * q.y + q.x * q.x)
        // ) * Mathf.Rad2Deg;

        // // Build new quaternion with pitch=0, roll=0
        // Quaternion desiredRot = Quaternion.AngleAxis(yaw, Vector3.up);

        // Quaternion q = transform.rotation;

        // // Extract forward direction from quaternion
        // Vector3 fwd = q * Vector3.forward;

        // // Remove vertical component (kills pitch)
        // fwd.y = 0f;

        // // If forward becomes zero (looking straight up/down), pick fallback
        // if (fwd.sqrMagnitude < 0.0001f)
        //     fwd = Vector3.forward;

        // // Rebuild quaternion with zero pitch
        // Quaternion flat = Quaternion.LookRotation(fwd, Vector3.up);

        // Quaternion desiredRot = flat;

        // var desired = QuaternionToYXZ.QuaternionToEulerYXZ(transform.rotation);

        // var desired = Unity.Mathematics.math.EulerYXZ(transform.rotation);
        // Quaternion desiredRot = Quaternion.Euler(0, desired.x * Mathf.Rad2Deg, 0);

        //transform.right

        // Sensitive to z as well => a problem

        // Vector3 testRotation = transform.rotation.eulerAngles;
        // testRotation.z = 0;
        // Quaternion quat = Quaternion.Euler(testRotation);
        
        // Problem only z axis, no longer x. Actually does not solve anything just moved x to z problem
        // Quaternion desiredRot = (transform.rotation * Vector3.up).y >= 0 ? Quaternion.Euler(0, transform.eulerAngles.y, 0) : Quaternion.Euler(0, transform.eulerAngles.y - 180, 0);

        // Quaternion desiredRot = (transform.rotation * Vector3.right).z >= 0 ? Quaternion.Euler(0, transform.eulerAngles.y, 0) : Quaternion.Euler(0, transform.eulerAngles.y - 180, 0);
        
        // Quaternion.

        // var projection = transform.rotation * Vector3.right;
        // Quaternion.FromToRotation(, Vector3.right);

        // var a = Quaternion.Euler(0, transform.eulerAngles.y, 0);
        // var b = Quaternion.Euler(0, transform.eulerAngles.y - 180, 0);

        // // var ra = Quaternion.FromToRotation();
        // var aa = Quaternion.Angle(transform.rotation, a);
        // var ab = Quaternion.Angle(transform.rotation, b);
        // Quaternion desiredRot = aa <= ab ? a : b;

        // var cur = transform.eulerAngles;
        // Quaternion desiredRot = Quaternion.Euler(0, cur.z >= 180 ? cur.y - 180 : cur.y, 0);

        // desiredRot.ToAngleAxis()

        // transform.rotation

        // desiredRot.eulerAngles = new Vector3(0, desiredRot.eulerAngles.y, 0);

        // (transform.rotation * Quaternion.FromToRotation(Vector3.right, Vector3.forward)).eulerAngles.z % 180 Real X value?

        var desiredRot = RemoveRollPitch(rb.rotation);

        // 3. Rotation difference
        // Quaternion delta = desiredRot * Quaternion.Inverse(rb.rotation);

        if(modify)
        {
            transform.rotation = rb.rotation * desiredRot;
        }

        // // Convert quaternion to axis-angle
        // delta.ToAngleAxis(out float angle, out Vector3 axis);

        // // Fix weird cases
        // if (float.IsNaN(axis.x)) return;

        // // Convert angle to radians and clamp
        // angle = Mathf.DeltaAngle(0, angle);

        // // 4. Apply torque
        // Vector3 acc = axis.normalized * angle * torqueStrength;

        // // // Add damping to prevent overshoot
        // // torque -= rb.angularVelocity * damping;

        // // ❗ Only rotate around X and Z
        // // acc.y = 0f;

        // // Add damping
        // acc -= new Vector3(rb.angularVelocity.x, rb.angularVelocity.y, rb.angularVelocity.z) * damping;

        // // acc.y = 0f;

        // // rb.AddTorque(acc, ForceMode.Acceleration);

        // // Calculate equivalent torque
        // Quaternion invRot = Quaternion.Inverse(rb.inertiaTensorRotation);
        // Vector3 localAcc = invRot * acc;

        // Vector3 localTorque = Vector3.Scale(rb.inertiaTensor, localAcc);
        // Vector3 torque = rb.inertiaTensorRotation * localTorque;

        // if(modify) {
        // // Debug.Log("Equivalent torque: " + torque);
        // // rb.AddTorque(torque, ForceMode.Force);
        // // rb.MoveRotation(desiredRot);
        // rb.MoveRotation(desiredRot);
        // }

        desiredRotation = desiredRot.eulerAngles;
        rawDesiredRotation = desiredRot.ToString();
        currentRotation = transform.rotation.eulerAngles;
        rawRotation = transform.rotation.ToString();
    }
}
