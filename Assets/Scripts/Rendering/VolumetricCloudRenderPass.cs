using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering.RenderGraphModule;

/// <summary>
/// URP ScriptableRendererFeature that injects the volumetric cloud rendering pass
/// after the skybox has been drawn but before opaque geometry.
///
/// Setup:
///   1. Create a material using the Custom/VolumetricClouds shader.
///   2. Add this feature to your URP Renderer asset (Project Settings → Graphics → URP Renderer).
///   3. Assign the cloud material to the <see cref="cloudMaterial"/> field.
///   4. Also assign the same material to WeatherManager.cloudMaterial and
///      DayNightCycle.cloudMaterial so all properties are routed correctly.
/// </summary>
[DisallowMultipleRendererFeature("Volumetric Cloud Render Feature")]
public class VolumetricCloudRenderFeature : ScriptableRendererFeature
{
    [Header("Cloud Material")]
    [Tooltip("Material using the Custom/VolumetricClouds shader. " +
             "Assign the same material to WeatherManager.cloudMaterial and DayNightCycle.cloudMaterial.")]
    public Material cloudMaterial;

    private VolumetricCloudPass _pass;

    /// <inheritdoc/>
    public override void Create()
    {
        _pass = new VolumetricCloudPass
        {
            renderPassEvent = RenderPassEvent.AfterRenderingSkybox
        };
    }

    /// <inheritdoc/>
    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        if (cloudMaterial == null)
        {
            Debug.LogWarning("[VolumetricCloudRenderFeature] cloudMaterial is not assigned. " +
                             "Assign a material using the Custom/VolumetricClouds shader.");
            return;
        }

        // Skip cameras that don't need volumetric clouds:
        //   - SceneView: editor overhead — clouds would render on every scene navigation
        //   - Reflection: reflection probes see only a static cube; raymarching is wasted
        //   - Preview:    Inspector material/model previews don't need clouds
        CameraType camType = renderingData.cameraData.cameraType;
        if (camType == CameraType.SceneView ||
            camType == CameraType.Reflection ||
            camType == CameraType.Preview)
            return;

        _pass.Setup(cloudMaterial);
        renderer.EnqueuePass(_pass);
    }

    /// <inheritdoc/>
    protected override void Dispose(bool disposing)
    {
        _pass?.Cleanup();
    }

    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Render pass that draws a fullscreen procedural triangle using the
    /// VolumetricClouds material, alpha-compositing clouds over the skybox.
    /// </summary>
    private class VolumetricCloudPass : ScriptableRenderPass
    {
        private const string k_Tag = "VolumetricClouds";
        private static readonly ProfilingSampler s_Sampler = new ProfilingSampler(k_Tag);

        private Material _material;

        public void Setup(Material mat) => _material = mat;
        public void Cleanup()           => _material = null;

        // ── RenderGraph path (URP 17 / Unity 6+) ─────────────────────────────

        /// <summary>Data passed into the RenderGraph render function.</summary>
        private class PassData
        {
            public Material material;
            public Matrix4x4 invProj;
            public Matrix4x4 invView;
        }

        /// <inheritdoc/>
        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            if (_material == null) return;

            UniversalCameraData   cameraData   = frameData.Get<UniversalCameraData>();
            UniversalResourceData resourceData = frameData.Get<UniversalResourceData>();

            Camera cam = cameraData.camera;

            // Same matrix logic as the legacy Execute() path.
            Matrix4x4 gpuProj = GL.GetGPUProjectionMatrix(cam.projectionMatrix, true);
            Matrix4x4 invProj = gpuProj.inverse;
            Matrix4x4 invView = cam.cameraToWorldMatrix;

            using (var builder = renderGraph.AddRasterRenderPass<PassData>(k_Tag, out PassData passData, s_Sampler))
            {
                passData.material = _material;
                passData.invProj  = invProj;
                passData.invView  = invView;

                // Write to the active camera colour texture (composite over skybox).
                builder.SetRenderAttachment(resourceData.activeColorTexture, 0);

                builder.SetRenderFunc(static (PassData data, RasterGraphContext ctx) =>
                {
                    // Set per-camera matrices so the shader can reconstruct world-space rays.
                    data.material.SetMatrix("_InvProjectionMatrix", data.invProj);
                    data.material.SetMatrix("_CloudCameraInvView",  data.invView);

                    // Draw a fullscreen procedural triangle; the material blends via
                    // SrcAlpha / OneMinusSrcAlpha so clouds composite naturally.
                    ctx.cmd.DrawProcedural(Matrix4x4.identity, data.material, 0,
                                           MeshTopology.Triangles, 3, 1);
                });
            }
        }

        // ── Legacy Compatibility Mode path (URP < 17 / RenderGraph disabled) ─

        /// <inheritdoc/>
#pragma warning disable CS0618, CS0672 // URP Compatibility Mode API — obsolete in URP 17+, retained for compatibility
        public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
        {
            // Attach to the camera colour target without clearing it.
            ConfigureTarget(renderingData.cameraData.renderer.cameraColorTargetHandle);
            ConfigureClear(ClearFlag.None, Color.black);
        }

        /// <inheritdoc/>
        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            if (_material == null) return;

            Camera cam = renderingData.cameraData.camera;

            // Build inverse projection/view matrices so the shader can reconstruct
            // world-space ray directions from screen-space coordinates.
            //
            // GL.GetGPUProjectionMatrix accounts for platform-specific clip-space
            // conventions (e.g. reversed Z on DX12/Vulkan, Y-flip on Metal).
            Matrix4x4 gpuProj    = GL.GetGPUProjectionMatrix(cam.projectionMatrix, true);
            Matrix4x4 invProj    = gpuProj.inverse;
            Matrix4x4 invView    = cam.cameraToWorldMatrix;   // camera → world (rotation + translation)

            _material.SetMatrix("_InvProjectionMatrix", invProj);
            _material.SetMatrix("_CloudCameraInvView",  invView);

            CommandBuffer cmd = CommandBufferPool.Get(k_Tag);
            using (new ProfilingScope(cmd, s_Sampler))
            {
                // Draw a full-screen triangle procedurally.
                // The vertex shader generates NDC positions from SV_VertexID (no mesh required).
                // The material uses SrcAlpha / OneMinusSrcAlpha blend, so clouds composite
                // naturally over whatever was already in the colour buffer (the skybox).
                cmd.DrawProcedural(Matrix4x4.identity, _material, 0,
                                   MeshTopology.Triangles, 3, 1);
            }

            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }
#pragma warning restore CS0618, CS0672

        /// <inheritdoc/>
        public override void OnCameraCleanup(CommandBuffer cmd) { }
    }
}
