using System;
using System.ComponentModel;
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
    private MaterialPropertyBlock m_block = null;

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
        m_block ??= new();
        m_block.SetTexture("leftSDF", leftSDF);
        m_block.SetTexture("rightSDF", rightSDF);
        m_block.SetVector("leftPosition", leftTransform.position);
        m_block.SetVector("rightPosition", rightTransform.position);
        m_block.SetMatrix("leftProjection", leftProjection);
        m_block.SetMatrix("rightProjection", rightProjection);
        camera.GetUniversalAdditionalCameraData().scriptableRenderer.EnqueuePass(
            new RenderFeaturePass(
                new RenderFeatureSettings {
                    m_material = material,
                    m_materialPropertyBlock = m_block
                }
            ) {
                renderPassEvent = RenderPassEvent.AfterRenderingOpaques
            }
        );
    }

    // Use this class to pass around settings from the feature to the pass
    [Serializable]
    public class RenderFeatureSettings
    {
        public Material m_material;
        public MaterialPropertyBlock m_materialPropertyBlock;
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
            CoreUtils.DrawFullScreen(context.cmd, data.m_material, properties: data.m_materialPropertyBlock, shaderPassId: 0);
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
                passData.m_materialPropertyBlock = settings.m_materialPropertyBlock;

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
