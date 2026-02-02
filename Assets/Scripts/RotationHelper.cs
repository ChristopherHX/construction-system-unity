using UnityEngine;

public class QuaternionToYXZ
{
    // Convert Quaternion to Euler angles in YXZ order (degrees)
    public static Vector3 QuaternionToEulerYXZ(Quaternion q)
    {
        // Normalize quaternion to avoid numerical drift
        q.Normalize();

        // Extract rotation matrix elements
        float xx = q.x * q.x;
        float yy = q.y * q.y;
        float zz = q.z * q.z;
        float ww = q.w * q.w;

        float xy = q.x * q.y;
        float xz = q.x * q.z;
        float yz = q.y * q.z;
        float wx = q.w * q.x;
        float wy = q.w * q.y;
        float wz = q.w * q.z;

        // Rotation matrix from quaternion
        float m00 = ww + xx - yy - zz;
        float m01 = 2f * (xy - wz);
        float m02 = 2f * (xz + wy);

        float m10 = 2f * (xy + wz);
        float m11 = ww - xx + yy - zz;
        float m12 = 2f * (yz - wx);

        float m20 = 2f * (xz - wy);
        float m21 = 2f * (yz + wx);
        float m22 = ww - xx - yy + zz;

        // Extract YXZ angles
        float y, x, z;

        // Handle gimbal lock
        if (Mathf.Abs(m12) < 0.999999f)
        {
            y = Mathf.Asin(Mathf.Clamp(m12, -1f, 1f)); // X rotation in YXZ
            x = Mathf.Atan2(-m02, m22);
            z = Mathf.Atan2(-m10, m11);
        }
        else
        {
            // Gimbal lock case
            y = Mathf.Asin(Mathf.Clamp(m12, -1f, 1f));
            x = Mathf.Atan2(m20, m00);
            z = 0f;
        }

        // Convert to degrees
        return new Vector3(
            Mathf.Rad2Deg * x,
            Mathf.Rad2Deg * y,
            Mathf.Rad2Deg * z
        );
    }

    void Start()
    {
        Quaternion q = Quaternion.Euler(30f, 45f, 60f); // Example quaternion
        Vector3 eulerYXZ = QuaternionToEulerYXZ(q);
        Debug.Log($"YXZ Euler angles: {eulerYXZ}");
    }
}
