using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class LockRotation : MonoBehaviour
{
    private Rigidbody _rigidbody;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        _rigidbody = GetComponent<Rigidbody>();
    }

    // Update is called once per frame
    void Update()
    {
        _rigidbody.rotation = Quaternion.identity;
    }
}
