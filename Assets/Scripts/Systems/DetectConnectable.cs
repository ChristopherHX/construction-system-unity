using System.Linq;
using UnityEngine;

[RequireComponent(typeof(ConstructionInfo))]
public class DetectConnectable : MonoBehaviour
{
    private ConstructionInfo _constructionInfo;

    void OnEnable()
    {
        _constructionInfo = GetComponent<ConstructionInfo>();
    }

    // Update is called once per frame
    void Update()
    {
        foreach (var item in _constructionInfo.faces.SelectMany(f => f.rays.Select(r => new { f, r })))
        {
            //Debug.DrawRay(item.f.transform.TransformPoint(item.r.origin), item.f.transform.TransformDirection(item.r.direction));
            if(Physics.Raycast(new Ray(item.f.transform.TransformPoint(item.r.origin), item.f.transform.TransformDirection(item.r.direction)), out RaycastHit hit, 5) && hit.rigidbody != null) {
                Debug.Log(hit.point);
            }
        }
    }
}
