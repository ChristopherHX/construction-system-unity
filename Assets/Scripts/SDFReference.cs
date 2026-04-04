

using UnityEngine;

[ExecuteInEditMode]
public class Reference : MonoBehaviour
{
    public Texture3D texture;
    public float stepScale = 1;
    public float surfaceOffset;
    public bool useCustomColorRamp;

    // We should initialize this gradient before using it as a custom color ramp
    public Gradient customColorRampGradient;
}