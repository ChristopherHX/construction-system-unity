using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;

public class VisualizeSDF : MonoBehaviour
{
    public Material material;
    public Texture3D leftSDF;
    public Transform leftTransform;
    public Matrix4x4 leftProjection;
    public Texture3D rightSDF;
    public Transform rightTransform;
    public Matrix4x4 rightProjection;
    public bool useMockInput = false;
    public Vector3 mockLeftOffset = new(-0.8f, 0f, 0f);
    public Vector3 mockRightOffset = new(0.8f, 0f, 0f);
    [Min(0.001f)]
    public float mockRadius = 0.5f;
    [Range(0f, 5f)]
    public float debugMode = 0f;
    private int m_lastDebugLogFrame = -9999;

    void OnEnable()
    {
        RenderPipelineManager.beginCameraRendering += BeginCameraRendering;
    }

    void OnDisable()
    {
        RenderPipelineManager.beginCameraRendering -= BeginCameraRendering;
    }

    private void BeginCameraRendering(ScriptableRenderContext context, Camera camera)
    {
        if(material == null)
        {
            return;
        }

        bool hasLeft = false;
        bool hasRight = false;
        Matrix4x4 leftWorldToSdf;
        Matrix4x4 rightWorldToSdf;
        Vector3 leftCenter;
        Vector3 rightCenter;

        if(useMockInput)
        {
            leftCenter = transform.TransformPoint(mockLeftOffset);
            rightCenter = transform.TransformPoint(mockRightOffset);
            leftWorldToSdf = Matrix4x4.identity;
            rightWorldToSdf = Matrix4x4.identity;
            hasLeft = true;
            hasRight = true;
        }
        else
        {
            hasLeft = TryGetWorldToSdfMatrix(leftTransform, out leftWorldToSdf, out leftCenter);
            hasRight = TryGetWorldToSdfMatrix(rightTransform, out rightWorldToSdf, out rightCenter);
            if(!hasLeft)
            {
                leftCenter = leftTransform != null ? leftTransform.position : Vector3.zero;
                leftWorldToSdf = Matrix4x4.identity;
            }
            if(!hasRight)
            {
                rightCenter = rightTransform != null ? rightTransform.position : Vector3.zero;
                rightWorldToSdf = Matrix4x4.identity;
            }
        }

        material.SetTexture("leftSDF", leftSDF);
        material.SetTexture("rightSDF", rightSDF);
        material.SetVector("leftPosition", leftCenter);
        material.SetVector("rightPosition", rightCenter);
        material.SetMatrix("leftProjection", leftProjection);
        material.SetMatrix("rightProjection", rightProjection);
        material.SetMatrix("leftWorldToSdf", leftWorldToSdf);
        material.SetMatrix("rightWorldToSdf", rightWorldToSdf);
        material.SetVector("cameraForward", camera.transform.forward);
        material.SetFloat("_UseMockInput", useMockInput ? 1f : 0f);
        material.SetFloat("_MockRadius", Mathf.Max(mockRadius, 0.001f));
        material.SetFloat("_DebugMode", debugMode);

        if(debugMode > 0f && Time.frameCount - m_lastDebugLogFrame > 30)
        {
            m_lastDebugLogFrame = Time.frameCount;
            Debug.Log($"VisualizeSDF debug cam={camera.name} mode={debugMode} mock={useMockInput} leftTex={(leftSDF != null)} rightTex={(rightSDF != null)} leftMapped={hasLeft} rightMapped={hasRight} dist={Vector3.Distance(leftCenter, rightCenter):F3}");
        }

        camera.GetUniversalAdditionalCameraData().scriptableRenderer.EnqueuePass(
            new RenderFeaturePass(
                new RenderFeatureSettings {
                    m_material = material
                }
            ) {
                renderPassEvent = RenderPassEvent.AfterRendering
            }
        );
    }

    private static bool TryGetWorldToSdfMatrix(Transform source, out Matrix4x4 worldToSdf, out Vector3 worldCenter)
    {
        worldToSdf = Matrix4x4.identity;
        worldCenter = Vector3.zero;

        if(source == null)
        {
            return false;
        }

        MeshFilter meshFilter = source.GetComponentInChildren<MeshFilter>();
        if(meshFilter != null && meshFilter.sharedMesh != null)
        {
            Bounds meshBounds = meshFilter.sharedMesh.bounds;
            Vector3 reciprocalSize = ReciprocalSafe(meshBounds.size);
            Matrix4x4 meshLocalToSdf = Matrix4x4.Scale(reciprocalSize) * Matrix4x4.Translate(-meshBounds.center);
            worldToSdf = meshLocalToSdf * meshFilter.transform.worldToLocalMatrix;
            worldCenter = meshFilter.transform.TransformPoint(meshBounds.center);
            return true;
        }

        Renderer renderer = source.GetComponentInChildren<Renderer>();
        if(renderer != null)
        {
            Bounds worldBounds = renderer.bounds;
            Vector3 reciprocalSize = ReciprocalSafe(worldBounds.size);
            worldToSdf = Matrix4x4.Scale(reciprocalSize) * Matrix4x4.Translate(-worldBounds.center);
            worldCenter = worldBounds.center;
            return true;
        }

        worldToSdf = source.worldToLocalMatrix;
        worldCenter = source.position;
        return true;
    }

    private static Vector3 ReciprocalSafe(Vector3 value)
    {
        const float epsilon = 1e-6f;
        return new Vector3(
            Mathf.Abs(value.x) > epsilon ? 1f / value.x : 1f,
            Mathf.Abs(value.y) > epsilon ? 1f / value.y : 1f,
            Mathf.Abs(value.z) > epsilon ? 1f / value.z : 1f
        );
    }

    // Use this class to pass around settings from the feature to the pass
    [Serializable]
    public class RenderFeatureSettings
    {
        public Material m_material;
    }

    class RenderFeaturePass : ScriptableRenderPass
    {
        readonly RenderFeatureSettings settings;

        public RenderFeaturePass(RenderFeatureSettings settings)
        {
            this.settings = settings;
        }

        // This static method is passed as the RenderFunc delegate to the RenderGraph render pass.
        // It is used to execute draw commands.
        static void ExecutePass(RenderFeatureSettings data, RasterGraphContext context)
        {
            CoreUtils.DrawFullScreen(context.cmd, data.m_material, shaderPassId: 0);
        }

        // RecordRenderGraph is where the RenderGraph handle can be accessed, through which render passes can be added to the graph.
        // FrameData is a context container through which URP resources can be accessed and managed.
        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            const string passName = "Draw Slimke";

            // var leftSDFHandle = renderGraph.ImportTexture(settings.leftSDF);

            // This adds a raster render pass to the graph, specifying the name and the data type that will be passed to the ExecutePass function.
            using (var builder = renderGraph.AddRasterRenderPass<RenderFeatureSettings>(passName, out var passData))
            {
                // Use this scope to set the required inputs and outputs of the pass and to
                // setup the passData with the required properties needed at pass execution time.
                passData.m_material = settings.m_material;

                // Make use of frameData to access resources and camera data through the dedicated containers.
                // Eg:
                // UniversalCameraData cameraData = frameData.Get<UniversalCameraData>();
                UniversalResourceData resourceData = frameData.Get<UniversalResourceData>();

                // builder.UseTexture(settings.leftSDF, AccessFlags.Read);

                // Setup pass inputs and outputs through the builder interface.
                // Eg:
                // builder.UseTexture(sourceTexture);
                // TextureHandle destination = UniversalRenderer.CreateRenderGraphTexture(renderGraph, cameraData.cameraTargetDescriptor, "Destination Texture", false);

                // This sets the render target of the pass to the active color texture. Change it to your own render target as needed.
                builder.SetRenderAttachment(resourceData.activeColorTexture, 0);

                // Assigns the ExecutePass function to the render pass delegate. This will be called by the render graph when executing the pass.
                builder.SetRenderFunc((RenderFeatureSettings data, RasterGraphContext context) => ExecutePass(data, context));
            }
        }
    }
}
