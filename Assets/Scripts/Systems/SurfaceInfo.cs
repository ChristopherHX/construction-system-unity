using System.Linq;
using UnityEngine;

public class SurfaceInfo : MonoBehaviour
{
    [SerializeField]
    private Texture2D texture;

    [SerializeField]
    public Ray[] rays;

    void OnEnable()
    {
        rays = MeshTextureRegions.Extract(texture, GetComponent<MeshFilter>().sharedMesh, 0.3f).Select(r => new Ray(r.localPos, r.localNormal)).ToArray();
    }
}