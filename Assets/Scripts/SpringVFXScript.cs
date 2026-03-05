using Unity.Mathematics;
using UnityEngine;
using UnityEngine.VFX;

[RequireComponent(typeof(VisualEffect))]
public class SpringVFXScript : MonoBehaviour
{
    private GraphicsBuffer transforms;
    // private float[] buf = new float[3];
    private Vector3[] buf;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        int count = transform.childCount;
        transforms = new GraphicsBuffer(GraphicsBuffer.Target.Structured, count, sizeof(float)*3);
        buf = new Vector3[count];
        GetComponent<VisualEffect>().SetGraphicsBuffer("transforms", transforms);
    }

    // Update is called once per frame
    void LateUpdate()
    {
        for(int i = 0; i < transform.childCount; i++)
        {
            var t = transform.GetChild(i);
            // buf[0] = t.localPosition.x;
            // buf[1] = t.localPosition.y;
            // buf[2] = t.localPosition.z;
            // transforms.SetData(buf, 0, i, 3);
            buf[i] = t.localPosition;
        }
        transforms.SetData(buf);
    }
}
