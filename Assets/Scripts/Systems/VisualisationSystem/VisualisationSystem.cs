using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace VisualisationSystem
{
    public class VisualisationSystem
    {
        public Material Construct { get; set; }
        public Material Selection { get; set; }

        public void ShowConnection(GameObject left, GameObject right, bool enabled = true)
        {
            
        }

        public void ShowRotateUI(GameObject self, bool enabled = true)
        {
            
        }

        public enum HighlightMode
        {
            None,
            Construct,
            Selection
        }

        public void Highlight(GameObject obj, HighlightMode mode)
        {
            var renderer = obj.GetComponent<Renderer>();
            List<Material> materials = new List<Material> { renderer.materials.First() };
            switch(mode)
            {
                case HighlightMode.Construct:
                    if(Construct == null)
                    {
                        Debug.LogError("Missing Construct Material");
                        return;
                    }
                    // Yellow material
                    materials.Add(Construct);
                    break;
                case HighlightMode.Selection:
                    if(Selection == null)
                    {
                        Debug.LogError("Missing Selection Material");
                        return;
                    }
                    // Green material
                    materials.Add(Selection);
                    break;
            }
            renderer.SetMaterials(materials);
        }
        public void Highlight(ConstructionInfo obj, HighlightMode mode)
        {
            foreach (var item in obj.faces)
            {
                Highlight(item.gameObject, mode);
            }
        }
    }
}