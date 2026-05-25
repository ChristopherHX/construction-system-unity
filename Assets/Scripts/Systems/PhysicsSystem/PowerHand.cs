using UnityEngine;

namespace PhysicsSystem
{
    public class PowerHand : MonoBehaviour
    {
        public Transform follow;
        private ConfigurableJoint _joint;
        private Rigidbody _moveRigidbody;
        private ConfigurableJoint _moveJoint;

        private Rigidbody createMoveObject()
        {
            var moveObj = new GameObject();
            _moveRigidbody = moveObj.AddComponent<Rigidbody>();
            _moveRigidbody.useGravity = false;
            // Workaround that the anchor rotates due to mass of the target object
            _moveRigidbody.freezeRotation = true;
            _moveJoint = moveObj.AddComponent<ConfigurableJoint>();
            _moveJoint.autoConfigureConnectedAnchor = false;
            _moveJoint.xMotion = ConfigurableJointMotion.Free;
            _moveJoint.yMotion = ConfigurableJointMotion.Free;
            _moveJoint.zMotion = ConfigurableJointMotion.Free;
            _moveJoint.angularXMotion = ConfigurableJointMotion.Locked;
            _moveJoint.angularYMotion = ConfigurableJointMotion.Locked;
            _moveJoint.angularZMotion = ConfigurableJointMotion.Locked;
            var drive = new JointDrive();
            drive.positionSpring = 1000000;
            drive.positionDamper = 1000000;
            drive.maximumForce = 10000000;
            drive.useAcceleration = true;
            _moveJoint.xDrive = drive;
            _moveJoint.yDrive = drive;
            _moveJoint.zDrive = drive;

            return _moveRigidbody;
        }

        void OnEnable()
        {
            // Rotate 90 Degree to right
            _joint = gameObject.AddComponent<ConfigurableJoint>();
            _joint.connectedBody = createMoveObject();
            _joint.autoConfigureConnectedAnchor = false;
            _joint.anchor = transform.InverseTransformPoint(_moveJoint.transform.position);
            _joint.connectedAnchor = Vector3.zero;

            _joint.xMotion = ConfigurableJointMotion.Limited;
            _joint.yMotion = ConfigurableJointMotion.Limited;
            _joint.zMotion = ConfigurableJointMotion.Limited;

            _joint.angularXMotion = ConfigurableJointMotion.Limited;
            _joint.angularYMotion = ConfigurableJointMotion.Free;
            _joint.angularZMotion = ConfigurableJointMotion.Limited;
            _joint.rotationDriveMode = RotationDriveMode.Slerp;
            _joint.enableCollision = true;

            SoftJointLimit limit = _joint.linearLimit;
            limit.limit = 0;
            _joint.linearLimit = limit;
            // Rotate 90 Degree to right
            // _joint.targetRotation = Quaternion.AngleAxis(-90, Vector3.up) * transform.rotation;

            JointDrive drive = new JointDrive
            {
                positionSpring = 200000f,
                positionDamper = 200000f,
                maximumForce = 1000000f
            };

            _joint.slerpDrive = drive;

            // Optional: helps stabilize orientation behavior.
            _joint.configuredInWorldSpace = false;
        }

        void FixedUpdate()
        {
            _joint.anchor = transform.InverseTransformPoint(_moveJoint.transform.position);
            if(follow != null)
            {
                _moveJoint.targetPosition = follow.position;
            }
            // _moveRigidbody.centerOfMass = _moveRigidbody.transform.InverseTransformPoint(transform.position);
        }

        void OnDisable()
        {
            Destroy(_joint);
            Destroy(_moveRigidbody.gameObject);
        }
    }
}