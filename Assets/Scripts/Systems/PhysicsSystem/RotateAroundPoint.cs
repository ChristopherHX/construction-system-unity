using UnityEngine;

namespace PhysicsSystem
{
    public class RotateAroundPoint : MonoBehaviour
    {
        public Transform pivotPoint; // The point you want to rotate around
        public Vector3 direction;
        public float forceMagnitude = 10f;

        void FixedUpdate()
        {
            if(pivotPoint == null || direction == null)
            {
                return;
            }
            // Apply force at a position offset from the pivot
            GetComponent<Rigidbody>().AddForceAtPosition(direction * forceMagnitude, pivotPoint.position, ForceMode.Force);
        }
    }
}