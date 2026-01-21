using System;
using Unity.VisualScripting;
using UnityEngine;

public class Connector : MonoBehaviour
{
    [SerializeField] MyButton clickMe = new MyButton() { OnButtonClicked = () => Debug.Log("Clicked!!") };

    private void Clicked()
    {
        
    }
    // [Property] Action myButton2 = Update;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private void HandleCollission(Collision collision, bool enter)
    {
        if(collision.collider.gameObject.TryGetComponent<Renderer>(out var renderer))
        {
            renderer.material.color = enter ? Color.red : Color.aquamarine;
        }
    }

    void OnCollisionEnter(Collision collision)
    {
        HandleCollission(collision, true);
    }

    void OnCollisionStay(Collision collision)
    {
        HandleCollission(collision, true);
    }

    void OnCollisionExit(Collision collision)
    {
        HandleCollission(collision, false);
    }

    private void HandleCollider(Collider collider, bool enter)
    {
        if(collider.gameObject.TryGetComponent<Renderer>(out var renderer))
        {
            renderer.material.color = enter ? Color.red : Color.aquamarine;
        }
    }

    void OnTriggerEnter(Collider other)
    {
        HandleCollider(other, true);
    }

    void OnTriggerStay(Collider other)
    {
        HandleCollider(other, true);
    }

    void OnTriggerExit(Collider other)
    {
        HandleCollider(other, false);
    }
}
