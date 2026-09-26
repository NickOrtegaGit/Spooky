using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;

/// <summary>
/// Injects the Spooky/VhsDistortion fullscreen pass. Add this as a Renderer
/// Feature on Renderer2D and assign a material using that shader.
///
/// The feature only draws; VhsController decides how strong it is.
/// </summary>
public class VhsRenderFeature : ScriptableRendererFeature
{
    [SerializeField] private Material material;
    [SerializeField] private RenderPassEvent injectionPoint =
        RenderPassEvent.BeforeRenderingPostProcessing;

    private VhsPass pass;

    public override void Create()
    {
        if (material == null) return;

        pass = new VhsPass(material) { renderPassEvent = injectionPoint };
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        if (pass == null || material == null) return;

        // Scene view and preview cameras stay clean — the effect is for play.
        if (renderingData.cameraData.cameraType != CameraType.Game) return;

        // Off-screen stages rendering into a texture are content shown inside
        // the world, not the player's view of it — distorting them twice would
        // be wrong.
        if (renderingData.cameraData.camera.targetTexture != null) return;

        renderer.EnqueuePass(pass);
    }

    private class VhsPass : ScriptableRenderPass
    {
        private readonly Material material;

        public VhsPass(Material material)
        {
            this.material = material;
            requiresIntermediateTexture = true;
        }

        private class PassData
        {
            public TextureHandle source;
            public Material material;
        }

        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            var resources = frameData.Get<UniversalResourceData>();
            if (resources.isActiveTargetBackBuffer) return;

            TextureHandle source = resources.activeColorTexture;

            var descriptor = renderGraph.GetTextureDesc(source);
            descriptor.name = "VhsDistortionTarget";
            descriptor.clearBuffer = false;
            descriptor.depthBufferBits = 0;

            TextureHandle destination = renderGraph.CreateTexture(descriptor);

            using (var builder = renderGraph.AddRasterRenderPass<PassData>(
                "Spooky VHS Distortion", out var passData))
            {
                passData.source = source;
                passData.material = material;

                builder.UseTexture(source);
                builder.SetRenderAttachment(destination, 0);

                builder.SetRenderFunc((PassData data, RasterGraphContext context) =>
                {
                    Blitter.BlitTexture(context.cmd, data.source, new Vector4(1, 1, 0, 0),
                        data.material, 0);
                });
            }

            resources.cameraColor = destination;
        }
    }
}
