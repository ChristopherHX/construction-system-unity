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
        var startPos = transform.position - new Vector3(0, 0, GetComponent<Collider>().bounds.extents.z);
        if(other.attachedRigidbody != null)
        {
            other.attachedRigidbody.AddForce(Vector3.forward * 1);
        }
    }
}
