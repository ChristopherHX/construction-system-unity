using UnityEngine;

public class ApplyTorque : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    public Vector3 torque;

    // Update is called once per frame
    void FixedUpdate()
    {
        GetComponent<Rigidbody>().AddTorque(torque);
        // GetComponent<Rigidbody>().angularVelocity = torque;
    }
}
