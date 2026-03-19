using UnityEngine;

public class SnapConnection : MonoBehaviour
{
    private RaycastHit[] raycastHits = new RaycastHit[10];

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        var hits = Physics.RaycastNonAlloc(transform.GetChild(1).transform.position, transform.GetChild(1).transform.up, raycastHits, 1, LayerMask.GetMask("Connection"));
        for(int i = 0; i < hits; i++)
        {
            var hit = raycastHits[i];
            
            transform.position += (hit.transform.position - transform.GetChild(1).transform.position) * Time.deltaTime;
        }
    }

}
