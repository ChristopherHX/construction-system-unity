using UnityEngine;

public class ConnectionCaster : MonoBehaviour
{
    private RaycastHit[] raycastHits = new RaycastHit[10];

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        // transform.position + 5 * transform.up
        Debug.DrawRay(transform.position, transform.up);

        var hits = Physics.RaycastNonAlloc(transform.position, transform.up, raycastHits, 1, LayerMask.GetMask("Connection"));
        for(int i = 0; i < hits; i++)
        {
            var hit = raycastHits[i];
            Debug.DrawLine(transform.position, hit.transform.position, Color.plum);
        }
    }
}
