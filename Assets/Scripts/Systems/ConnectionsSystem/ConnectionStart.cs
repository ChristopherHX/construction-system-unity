using System.Linq;
using UnityEngine;

public class ConnectionStart : MonoBehaviour
{
    private RaycastHit[] raycastHits = new RaycastHit[10];

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    private ConstructionInfo _constructionInfo;

    void OnEnable()
    {
        _constructionInfo = GetComponent<ConstructionInfo>();
    }

    private RaycastHit hit;
    private Vector3 dist = Vector3.positiveInfinity;

    private ConfigurableJoint _joint;

    [SerializeField] float reelSpeed = 8f;

    // Update is called once per frame
    void Update()
    {
        Vector3 self = Vector3.zero;
        Vector3 anchor = Vector3.zero;
        foreach (var item in _constructionInfo.faces.SelectMany(f => f.rays.Select(r => new { f, r })))
        {
            // Debug.DrawRay(item.f.transform.TransformPoint(item.r.origin), item.f.transform.TransformDirection(item.r.direction));
            var hits = Physics.RaycastNonAlloc(item.f.transform.TransformPoint(item.r.origin), item.f.transform.TransformDirection(item.r.direction), raycastHits, 5, LayerMask.GetMask("Connection"));
            for(int i = 0; i < hits; i++)
            {
                var ndist = raycastHits[i].transform.position - item.f.transform.TransformPoint(item.r.origin);
                if(ndist.magnitude >= dist.magnitude && hit.rigidbody != null)
                {
                    continue;
                }
                dist = ndist;

                if (_joint != null && raycastHits[i].rigidbody != raycastHits[i].rigidbody)
                {
                    Destroy(_joint);
                }
                self = item.r.origin;
                if(raycastHits[i].rigidbody.TryGetComponent<ConstructionInfo>(out var octr))
                {
                    foreach (var oitem in octr.faces.SelectMany(f => f.rays.Select(r => new { f, r })))
                    {
                        if(Physics.Raycast(oitem.f.transform.TransformPoint(oitem.r.origin), oitem.f.transform.TransformDirection(oitem.r.direction), out var hitInfo, 5, LayerMask.GetMask("Connection")) && hitInfo.rigidbody == item.f.GetComponent<Rigidbody>())
                        {
                            anchor = oitem.r.origin;
                        }
                    }
                }
                hit = raycastHits[i];
                
                //transform.position += (hit.transform.position - item.f.transform.TransformPoint(item.r.origin)) * Time.deltaTime;
            }
        }

        if(_joint == null && hit.rigidbody != null)
        {
            _joint = gameObject.AddComponent<ConfigurableJoint>();
            _joint.connectedBody = hit.rigidbody;
            _joint.autoConfigureConnectedAnchor = false;
            _joint.anchor = self;
            _joint.connectedAnchor = anchor;

            _joint.xMotion = ConfigurableJointMotion.Limited;
            _joint.yMotion = ConfigurableJointMotion.Limited;
            _joint.zMotion = ConfigurableJointMotion.Limited;

            _joint.angularXMotion = ConfigurableJointMotion.Free;
            _joint.angularYMotion = ConfigurableJointMotion.Free;
            _joint.angularZMotion = ConfigurableJointMotion.Free;
            _joint.enableCollision = true;

            float dist = Vector3.Distance(transform.position, hit.rigidbody.position);

            SoftJointLimit limit = _joint.linearLimit;
            limit.limit = dist;
            _joint.linearLimit = limit;
        }
    }

    void FixedUpdate()
    {
        if (_joint == null)
            return;

        SoftJointLimit limit = _joint.linearLimit;
        limit.limit = Mathf.Max(0f, limit.limit - reelSpeed * Time.fixedDeltaTime);
        _joint.linearLimit = limit;
        if(limit.limit == 0)
        {
            Destroy(_joint);
            Destroy(this);
        }
    }

}
