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
            _actions.PlayerBuilding.Abort.performed += OnAbort;
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
            _actions.PlayerBuilding.Abort.performed -= OnAbort;
            _actions.PlayerBuilding.Ok.performed -= OnOk;
        }

        private void OnAbort(InputAction.CallbackContext context)
        {
            if(hit.TryGetComponent<ConnectionStart>(out var cs))
            {
                Destroy(cs);
            }
            system.Highlight(hit, VisualisationSystem.VisualisationSystem.HighlightMode.None);
            if(hit.TryGetComponent<PowerHand>(out var powerHand)) {
                Destroy(powerHand);
            }
            hit = null;
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
                    if(hit.TryGetComponent<PowerHand>(out var powerHand)) {
                        Destroy(powerHand);
                    }
                    hit.AddComponent<ConnectionStart>();
                    hit = null;
                    return;
                }
                hit = hitInfo.collider.gameObject;
                
                hit.AddComponent<PowerHand>().follow = transform;
                // hit.AddComponent<ConnectionStart>();
                system.Highlight(hit, VisualisationSystem.VisualisationSystem.HighlightMode.Selection);

            }
        }

        private void OnToggleRotate(InputAction.CallbackContext context)
        {
            
        }

        private float yaw = 0;
        private float pitch = 20;
        // private float playerYaw = 0;

        void Update()
        {
            Debug.DrawLine(CameraRay.origin, CameraRay.origin + CameraRay.direction * 20, Color.aquamarine);
            var move = _actions.PlayerBuilding.Move.ReadValue<Vector2>();
            var look = _actions.PlayerBuilding.Look.ReadValue<Vector2>();

            // Using 89 to avoid any glitch at +-90°
            pitch = Math.Clamp((pitch + look.y) % 360, -89, 89);
            yaw = (yaw + look.x) % 360;

            cameraTarget.rotation = Quaternion.Euler(pitch, yaw, 0);

            // playerYaw = (playerYaw + move.x) % 360;
            var playerYaw = yaw;
            transform.rotation = Quaternion.Euler(0, (playerYaw - 90) % 360, 0);
            _characterController.Move(Quaternion.Euler(0, playerYaw, 0) * new Vector3(move.x * Time.deltaTime, 0, move.y * Time.deltaTime) * 5);
        }

    }
}