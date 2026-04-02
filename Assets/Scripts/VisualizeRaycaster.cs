using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(MeshCollider))]
public class VisualizeRaycaster : MonoBehaviour
{
    public Texture2D texture;
    private List<MeshTextureRegions.ColorRegion> _regions;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        _regions = MeshTextureRegions.Extract(texture, GetComponent<MeshCollider>().sharedMesh, 0.3f);
    }

    // Update is called once per frame
    void Update()
    {
        foreach (var item in _regions)
        {
            Debug.DrawRay(transform.TransformPoint(item.localPos), transform.TransformDirection(item.localNormal));
        }
    }
}
