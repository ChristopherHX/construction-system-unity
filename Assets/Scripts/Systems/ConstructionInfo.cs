using UnityEngine;

public class ConstructionInfo : MonoBehaviour
{
    public SurfaceInfo[] faces;

    void OnEnable()
    {
        faces = GetComponentsInChildren<SurfaceInfo>();
    }

    void FixedUpdate()
    {
        Vector3 centerOfMass = Vector3.zero;
        // float mass = 0;
        for(int j = 0; j < faces.Length; j++)
        {
            var rg = faces[j].GetComponent<Rigidbody>();
            centerOfMass += rg.worldCenterOfMass;
        }
        centerOfMass /= faces.Length;

        Debug.Log(centerOfMass);

        // Convert Every Joint connection to a single rigidbody parent / Use Kinematik? No
        // Add ForceAtPosition? No do not apply outside of object or still yes?

    }
}