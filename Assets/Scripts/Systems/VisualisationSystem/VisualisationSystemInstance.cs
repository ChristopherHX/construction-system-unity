using UnityEngine;

namespace VisualisationSystem
{
    public class VisualisationSystemInstance : MonoBehaviour
    {
        private VisualisationSystem system = new VisualisationSystem();

        public Material construct;
        public Material selection;

        private VisualisationSystem.HighlightMode _mode = VisualisationSystem.HighlightMode.None;
        public VisualisationSystem.HighlightMode mode = VisualisationSystem.HighlightMode.None;
        public ConstructionInfo info;

        void OnEnable()
        {
            system.Construct = construct;
            system.Selection = selection;
        }

        void Update()
        {
            if(system == null || info == null)
            {
                return;
            }
            system.Construct = construct;
            system.Selection = selection;
            if(_mode != mode)
            {
                _mode = mode;
                system.Highlight(info, mode);
            }
        }
    }
}