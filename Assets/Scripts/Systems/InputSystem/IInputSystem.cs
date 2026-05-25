using UnityEngine;

namespace InputSystem {

    public interface IInputSystem
    {
        public delegate void SelectionStartEvent(IInputSystem sender);
        public delegate void SelectionStopEvent(IInputSystem sender);
        public event SelectionStartEvent SelectionStart;
        public event SelectionStopEvent SelectionStop;
        public event SelectionStartEvent Selectioned;
        public event SelectionStartEvent Released;
        (Vector3, Vector3) GetPositionAndLookDirection();
        // Moved
    }
}