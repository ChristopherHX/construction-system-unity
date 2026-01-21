using System;
using Unity.VisualScripting;
using UnityEngine;

public class MoveRigidBody : MonoBehaviour
{
    [SerializeField] MyButton move = new MyButton();
    [SerializeField] Vector3 force;
    [SerializeField] Vector3 torque;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        move.OnButtonClicked = OnMoveButtonClick;
    }

    private static void StopMoving(Rigidbody rigidbody)
    {
        rigidbody.linearVelocity = Vector3.zero;
        rigidbody.angularVelocity = Vector3.zero;
        if(rigidbody.TryGetComponent<Joint>(out var joint))
        {
            StopMoving(joint.connectedBody);
        }
    }

    private void OnMoveButtonClick()
    {
        this.enabled ^= true;
        if(enabled == false)
        {
            StopMoving(GetComponent<Rigidbody>());
            // GetComponent<Rigidbody>().isKinematic = true;
            // GetComponent<Rigidbody>().Sleep();
            // GetComponent<Rigidbody>().linearVelocity = Vector3.zero;
            // GetComponent<Rigidbody>().angularVelocity = Vector3.zero;

            
            // GetComponent<Rigidbody>().WakeUp();
            // GetComponent<Rigidbody>().isKinematic = false;
        }
    }

    // Update is called once per frame
    void Update()
    {
        GetComponent<Rigidbody>().AddForce(force);
        GetComponent<Rigidbody>().AddTorque(torque);
    }
}
