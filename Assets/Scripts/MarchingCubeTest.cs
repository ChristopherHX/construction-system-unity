using UnityEngine;

public class MarchingCubesTest : MonoBehaviour
{
    public ComputeShader shader;
    public Texture3D sdfTex;

    public int resolution = 32;
    public float isoLevel = 0.0f;

    Mesh mesh;
    GraphicsBuffer vertexBuffer;
    GraphicsBuffer countBuffer;

    void Start()
    {
        int maxVerts = resolution * resolution * resolution * 3;

        vertexBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, maxVerts, sizeof(float) * 3);
        countBuffer  = new GraphicsBuffer(GraphicsBuffer.Target.Structured, 1, sizeof(uint));

        uint[] zero = { 0 };
        countBuffer.SetData(zero);

        int kernel = shader.FindKernel("March");

        shader.SetTexture(kernel, "_SDFTex", sdfTex);
        shader.SetBuffer(kernel, "_Vertices", vertexBuffer);
        shader.SetBuffer(kernel, "_VertexCount", countBuffer);

        shader.SetVector("_GridMin", new Vector3(0,0,0));
        shader.SetVector("_GridMax", new Vector3(1,1,1));
        shader.SetInts("_GridResolution", resolution, resolution, resolution);
        shader.SetFloat("_IsoLevel", isoLevel);

        shader.Dispatch(kernel,
            Mathf.CeilToInt(resolution / 4f),
            Mathf.CeilToInt(resolution / 4f),
            Mathf.CeilToInt(resolution / 4f)
        );

        // Read back vertex count
        uint[] countArr = new uint[1];
        countBuffer.GetData(countArr);
        int vertCount = (int)countArr[0];

        Vector3[] verts = new Vector3[vertCount];
        vertexBuffer.GetData(verts);

        mesh = new Mesh();
        mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;

        int[] indices = new int[vertCount];
        for (int i = 0; i < vertCount; i++) indices[i] = i;

        mesh.vertices = verts;
        mesh.SetIndices(indices, MeshTopology.Triangles, 0);

        GetComponent<MeshFilter>().mesh = mesh;
    }

    void OnDestroy()
    {
        vertexBuffer?.Dispose();
        countBuffer?.Dispose();
    }
}
