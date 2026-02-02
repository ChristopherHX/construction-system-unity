using UnityEngine;

public class SyncRotation : MonoBehaviour
{
    public Transform tracked;

    public enum Mode
    {
        None,
        RemoveRollPitch
    }

    public Mode mode;

    private Quaternion RemoveRollPitch(Quaternion quat)
    {
        return Quaternion.FromToRotation(quat * Vector3.up, Vector3.up);
    }

    void Update()
    {
        Quaternion desiredRot = tracked.rotation;
        switch(mode)
        {
        case Mode.RemoveRollPitch:
            desiredRot = tracked.rotation * RemoveRollPitch(tracked.rotation);
        break;
        }
        transform.rotation = desiredRot;
    }
}
