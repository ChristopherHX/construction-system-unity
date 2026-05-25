using System;
using PhysicsSystem;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;

namespace InputSystem {

    public class InputSystem : MonoBehaviour
    {
        private VisualisationSystem.VisualisationSystem system = new VisualisationSystem.VisualisationSystem();

        public VizOptions options;
        private InputSystem_Actions _actions;

        public Transform cameraTransform;
        public Transform cameraTarget;

        public CinemachineRotationComposer cinemachineRotationComposer;

        private CharacterController _characterController;

        public Ray CameraRay { get
            {
                return new Ray(cameraTransform.position + cameraTransform.forward * 5, cameraTransform.forward);
            }
        }

        void OnEnable()
        {
            system.Construct = options?.Highlight;
            system.Selection = options?.Selection;
            _actions ??= new InputSystem_Actions();
            _actions.PlayerBuilding.StartBuilding.performed += OnStartBuilding;
            _actions.PlayerBuilding.ToggleRotate.performed += OnToggleRotate;
            _actions.PlayerBuilding.Ok.performed += OnOk;
            _characterController = GetComponent<CharacterController>();
            _actions.PlayerBuilding.Enable();
        }

        Collider[] _hits = new Collider[50];

        public ConstructionInfo[] _constructs = new ConstructionInfo[50];
        public int _nConstructs = 0;

        private void OnStartBuilding(InputAction.CallbackContext context)
        {
            _nConstructs = 0;
            for(int i = 0, cnt = Physics.OverlapSphereNonAlloc(transform.position, 10, _hits, ~LayerMask.GetMask("Player")); i < cnt; i++)
            {
                if(_hits[i].TryGetComponent<ConstructionInfo>(out var cnst))
                {
                    // Highlight
                    _constructs[_nConstructs++] = cnst;
                }
            }
        }

        void OnDisable()
        {
            _actions.PlayerBuilding.Disable();
            _actions.PlayerBuilding.ToggleRotate.performed -= OnToggleRotate;
            _actions.PlayerBuilding.StartBuilding.performed -= OnStartBuilding;
            _actions.PlayerBuilding.Ok.performed -= OnOk;
        }

        public GameObject hit;

        private void OnOk(InputAction.CallbackContext context)
        {
            if(Physics.Raycast(CameraRay, out var hitInfo, 20, ~LayerMask.GetMask("Player")))
            {
                if(hit != null)
                {
                    if(hit.TryGetComponent<ConnectionStart>(out var cs))
                    {
                        Destroy(cs);
                    }
                    system.Highlight(hit, VisualisationSystem.VisualisationSystem.HighlightMode.None);
                }
                hit = hitInfo.collider.gameObject;
                // if(hit.TryGetComponent<Rigidbody>(out var rg))
                // {
                //     rg.AddExplosionForce(10, hitInfo.point, 2);
                // }

                
                hit.AddComponent<PowerHand>();//.follow = transform;
                // hit.AddComponent<ConnectionStart>();
                system.Highlight(hit, VisualisationSystem.VisualisationSystem.HighlightMode.Selection);

                // // Rotate 90 Degree to right
                // var _joint = hit.AddComponent<ConfigurableJoint>();
                // _joint.connectedBody = null;
                // _joint.autoConfigureConnectedAnchor = false;
                // _joint.anchor = hit.transform.InverseTransformPoint(transform.position);
                // _joint.connectedAnchor = transform.position;

                // _joint.xMotion = ConfigurableJointMotion.Limited;
                // _joint.yMotion = ConfigurableJointMotion.Limited;
                // _joint.zMotion = ConfigurableJointMotion.Limited;
                // var jt = _joint.zDrive;
                // jt.maximumForce = 10000f;
                // jt.positionSpring = 2000;
                // jt.positionDamper = 2000;
                // _joint.zDrive = jt;
                // _joint.targetPosition = - 5*Vector3.up;

                // _joint.angularXMotion = ConfigurableJointMotion.Limited;
                // _joint.angularYMotion = ConfigurableJointMotion.Free;
                // _joint.angularZMotion = ConfigurableJointMotion.Limited;
                // _joint.rotationDriveMode = RotationDriveMode.Slerp;
                // _joint.enableCollision = true;

                // float dist = Vector3.Distance(transform.position, hit.transform.position);

                // SoftJointLimit limit = _joint.linearLimit;
                // limit.limit = 0;
                // _joint.linearLimit = limit;
                // // Rotate 90 Degree to right
                // _joint.targetRotation = Quaternion.AngleAxis(-90, Vector3.up) * transform.rotation;

                // JointDrive drive = new JointDrive
                // {
                //     positionSpring = 2000f,
                //     positionDamper = 2000f,
                //     maximumForce = 10000f
                // };

                // _joint.slerpDrive = drive;

                // // Optional: helps stabilize orientation behavior.
                // _joint.configuredInWorldSpace = false;
            }
        }

        private void OnToggleRotate(InputAction.CallbackContext context)
        {
            
        }

        private float yaw = 0;
        private float pitch = 20;

        void Update()
        {
            Debug.DrawLine(CameraRay.origin, CameraRay.origin + CameraRay.direction * 20, Color.aquamarine);
            var move = _actions.PlayerBuilding.Move.ReadValue<Vector2>();
            _characterController.Move(new Vector3(move.x * Time.deltaTime, 0, move.y * Time.deltaTime));
            var look = _actions.PlayerBuilding.Look.ReadValue<Vector2>();

            // Using 89 to avoid any glitch at +-90°
            pitch = Math.Clamp((pitch + look.y) % 360, -89, 89);
            yaw = (yaw + look.x) % 360;

            // var crotation = /*Quaternion.AngleAxis(look.x, Vector3.up) */ cameraTarget.rotation;
            // // var oldX = crotation.eulerAngles.x;
            // // var a = 180 - oldX;
            // cameraTarget.rotation = Quaternion.AngleAxis(look.y /*Mathf.Clamp(look.y, Mathf.Min(a, -a), Mathf.Max(a, -a))*/, Vector3.right) * crotation;
            // cinemachineRotationComposer.TargetOffset += new Vector3(0, look.y * Time.deltaTime, 0);
            cameraTarget.rotation = Quaternion.Euler(pitch, yaw, 0);
            //cinemachineRotationComposer.GetComponent<CinemachineThirdPersonAim>().
        }

    }
}