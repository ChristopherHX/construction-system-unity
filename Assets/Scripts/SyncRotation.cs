using UnityEngine;

public class SyncRotation : MonoBehaviour
{
    public Transform tracked;

    public enum Mode
    {
        None,
        RemoveRollPitch,
        RemoveRollPitch2,
        RemoveRollPitch3,
        RemoveRollPitch4,
        RemoveRollPitch1_1,
        RemoveRollPitch1_2,
    }

    public Mode mode;

    private Quaternion RemoveRollPitch(Quaternion quat)
    {
        var result = Quaternion.FromToRotation(quat * Vector3.up, Vector3.up);
        result.ToAngleAxis(out var angle, out var axis);
        Debug.Log($"PRED {angle} / {axis} / {quat * Vector3.up}");
        // quat.ToAngleAxis(out angle, out axis);
        // Debug.Log($"EXP Y 0 {angle} / {axis}");
        result = Quaternion.AngleAxis(angle, axis);
        return result;
    }

    private Quaternion RemoveRollPitch3(Quaternion quat)
    {
        // var removeX = quat * Quaternion.AngleAxis(quat.eulerAngles.x, Vector3.right);
        // if(removeX.eulerAngles.x > 1)
        // {
        //     removeX = quat * Quaternion.AngleAxis(-quat.eulerAngles.x, Vector3.right);
        // }
        // var removeXZ = removeX * Quaternion.AngleAxis(removeX.eulerAngles.z, Vector3.forward);
        // if(removeXZ.eulerAngles.z > 1)
        // {
        //     removeXZ = removeX * Quaternion.AngleAxis(-removeX.eulerAngles.z, Vector3.forward);
        // }
        // var direction = removeXZ * Vector3.up;
        // var removeFlip = direction.y >= 0 ? removeXZ : removeXZ * Quaternion.AngleAxis(180, Vector3.forward);
        var removeZ = quat * Quaternion.AngleAxis(quat.eulerAngles.z, Vector3.forward);
        if(removeZ.eulerAngles.z > 1)
        {
            removeZ = quat * Quaternion.AngleAxis(-quat.eulerAngles.z, Vector3.forward);
        }
        var removeXZ = removeZ * Quaternion.AngleAxis(removeZ.eulerAngles.x, Vector3.right);
        if(removeXZ.eulerAngles.x > 1)
        {
            removeXZ = removeZ * Quaternion.AngleAxis(-removeZ.eulerAngles.x, Vector3.right);
        }
        var direction = removeXZ * Vector3.up;
        var removeFlip = direction.y >= 0 ? removeXZ : removeXZ * Quaternion.AngleAxis(180, Vector3.forward);

        // tracked.up = quat * Vector3.up
        // return Quaternion.FromToRotation(quat * tracked.up, Vector3.up);
        return removeXZ.eulerAngles.z == 180 ? removeXZ * Quaternion.AngleAxis(180, Vector3.forward) : removeXZ;
    }

    void Update()
    {
        Quaternion desiredRot = tracked.rotation;
        switch(mode)
        {
        case Mode.RemoveRollPitch:
            desiredRot = tracked.rotation * RemoveRollPitch(tracked.rotation);
        break;
        case Mode.RemoveRollPitch2:
            desiredRot = RemoveRollPitch(tracked.rotation);
        break;
        case Mode.RemoveRollPitch3:
            desiredRot = tracked.rotation * RemoveRollPitch3(tracked.rotation);
        break;
        case Mode.RemoveRollPitch4:
            desiredRot = RemoveRollPitch3(tracked.rotation);
        break;
        case Mode.RemoveRollPitch1_1:
            desiredRot = Quaternion.Inverse(RemoveRollPitch(tracked.rotation));
        break;
        case Mode.RemoveRollPitch1_2:
            // This removes the pitch?
            desiredRot = RemoveRollPitch(tracked.rotation) * tracked.rotation;///*Quaternion.AngleAxis(-45, Vector3.up) */ Quaternion.Inverse(RemoveRollPitch(tracked.rotation)) * Quaternion.AngleAxis(45, Vector3.up);
        break;
        }
        transform.localRotation = desiredRot;
    }
}
