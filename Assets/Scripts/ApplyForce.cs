using UnityEngine;

public class ApplyForce : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    public Vector3 force;

    // Update is called once per frame
    void FixedUpdate()
    {
        GetComponent<Rigidbody>().AddRelativeForce(force);
        // GetComponent<Rigidbody>().angularVelocity = torque;
    }
}
