using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;

/// <summary>
/// Blacks out everything outside the local player's current room.
///
/// Add this as a Renderer Feature on Renderer2D, ORDERED BEFORE
/// VhsRenderFeature so the distortion applies to the masked image rather than
/// a clean one. Assign a material using Spooky/RoomOcclusion.
///
/// The room mesh comes from RoomTracker, which decides the current room; this
/// only draws. See Docs/House Layout.md.
/// </summary>
public class RoomOcclusionFeature : ScriptableRendererFeature
{
    [SerializeField] private Material material;

    [Tooltip("Before the VHS pass, so the mask is distorted along with the world.")]
    [SerializeField] private RenderPassEvent injectionPoint =
        RenderPassEvent.BeforeRenderingPostProcessing;

    private RoomOcclusionPass pass;

    public override void Create()
    {
        if (material == null) return;

        pass = new RoomOcclusionPass(material) { renderPassEvent = injectionPoint };
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        if (pass == null || material == null) return;

        // Scene view and previews stay unmasked — the occlusion is for play.
        if (renderingData.cameraData.cameraType != CameraType.Game) return;

        // Cameras rendering into a texture are off-screen stages — the dish
        // minigame, say — not the player's view. They sit outside every room,
        // so masking them would black them out entirely.
        if (renderingData.cameraData.camera.targetTexture != null) return;

        // Nothing to mask until a room is current.
        if (RoomTracker.Instance == null || RoomTracker.Instance.CurrentRoom == null) return;

        renderer.EnqueuePass(pass);
    }

    private class RoomOcclusionPass : ScriptableRenderPass
    {
        private static readonly int FadeAlphaId = Shader.PropertyToID("_FadeAlpha");

        private readonly Material material;

        public RoomOcclusionPass(Material material)
        {
            this.material = material;
        }

        private class PassData
        {
            public Material material;
            public Mesh roomMesh;
            public Mesh previousRoomMesh;
            public float transitionProgress;
        }

        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            var resources = frameData.Get<UniversalResourceData>();
            if (resources.isActiveTargetBackBuffer) return;

            Room room = RoomTracker.Instance != null ? RoomTracker.Instance.CurrentRoom : null;
            if (room == null) return;

            Mesh mesh = RoomTracker.Instance.CurrentRoomMesh;
            if (mesh == null) return;

            using (var builder = renderGraph.AddRasterRenderPass<PassData>(
                "Spooky Room Occlusion", out var passData))
            {
                passData.material = material;
                passData.roomMesh = mesh;
                passData.previousRoomMesh = RoomTracker.Instance.PreviousRoomMesh;
                passData.transitionProgress = RoomTracker.Instance.TransitionProgress;

                builder.SetRenderAttachment(resources.activeColorTexture, 0);
                builder.SetRenderAttachmentDepth(resources.activeDepthTexture);
                builder.AllowPassCulling(false);

                builder.SetRenderFunc((PassData data, RasterGraphContext context) =>
                {
                    // Pass 0 stencils each visible room's shape; pass 1 fills
                    // everything the stencil did not mark. Meshes are already
                    // in world space, so they draw with identity.
                    context.cmd.DrawMesh(data.roomMesh, Matrix4x4.identity, data.material, 0, 0);

                    // Mid-transition the room just left is stencilled too, so
                    // both stay visible while it eases out — otherwise the
                    // screen blinks black between rooms.
                    if (data.previousRoomMesh != null)
                    {
                        context.cmd.DrawMesh(data.previousRoomMesh, Matrix4x4.identity,
                            data.material, 0, 0);
                    }

                    context.cmd.DrawProcedural(Matrix4x4.identity, data.material, 1,
                        MeshTopology.Triangles, 3, 1);

                    // Then black out the old room alone, ramping to fully opaque
                    // as the transition completes.
                    if (data.previousRoomMesh != null)
                    {
                        data.material.SetFloat(FadeAlphaId, data.transitionProgress);
                        context.cmd.DrawMesh(data.previousRoomMesh, Matrix4x4.identity,
                            data.material, 0, 2);
                    }
                });
            }
        }
    }
}
