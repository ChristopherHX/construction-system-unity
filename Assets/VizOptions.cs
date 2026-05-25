using UnityEngine;

[CreateAssetMenu(fileName = "VizOptions", menuName = "Scriptable Objects/VizOptions")]
public class VizOptions : ScriptableObject
{
    public Material Highlight; 
    public Material Selection;
}
