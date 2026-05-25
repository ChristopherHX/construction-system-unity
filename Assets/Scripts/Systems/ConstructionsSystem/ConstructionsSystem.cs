using UnityEngine;

namespace ConstructionSystem
{
    public class Constructionssystem
    {
        private VisualisationSystem.VisualisationSystem visualisationSystem = new();

        void FindSelection(Ray ray)
        {
            if(Physics.Raycast(ray, out RaycastHit hitInfo, 5))
            {
                //visualisationSystem.Highlight();
                // Track target per player / only one
            }
        }

        void Connect(GameObject left, GameObject right)
        {
            // Call Connection System once pressed Connect
            // 
        }

        void Disconnect(GameObject left, GameObject right)
        {
            
        }
    }
}