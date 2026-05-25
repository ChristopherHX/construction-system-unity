using UnityEngine;

namespace PhysicsSystem
{
    public class PhysicsSystem
    {
        public void MoveConstruct(GameObject obj, Vector3 direction)
        {
            
        }
        public void RotateConstruct(GameObject obj, Vector3 towards)
        {
            foreach (var item in obj.GetComponent<ConstructionInfo>().faces)
            {
                var rd = item.GetComponent<Rigidbody>();
                // rd.AddTorque()
            }
        }
    }
}