using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class LockRotation : MonoBehaviour
{
    [SerializeField] MyButton myButton;
    
    private Rigidbody _rigidbody;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        _rigidbody = GetComponent<Rigidbody>();
        myButton.OnButtonClicked = () => Debug.Log("Clicked");
    }

    // Update is called once per frame
    void Update()
    {
        _rigidbody.rotation = Quaternion.identity;

        // Torque ist Drehmoment
        _rigidbody.AddTorque(new Vector3(1, 2, 3));
    }
}
