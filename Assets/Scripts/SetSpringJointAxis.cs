using UnityEngine;

public class SetSpringJointAxis : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    [SerializeField]
    private Vector3 axis;

    // Update is called once per frame
    void Update()
    {
        var joint = GetComponent<SpringJoint>();
        joint.axis = axis;
    }
}
