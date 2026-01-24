using UnityEngine;

public class WindMove : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    void OnTriggerStay(Collider other)
    {
        if(other.attachedRigidbody != null)
        {
            other.attachedRigidbody.AddForce(Vector3.forward * 1);
        }
    }
}
