using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

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

        /// <inheritdoc/>
#pragma warning disable CS0618 // URP Compatibility Mode API — obsolete in URP 17+, retained for compatibility
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
            Matrix4x4 gpuProj    = GL.GetGPUProjectionMatrix(cam.projectionMatrix, false);
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
#pragma warning restore CS0618

        /// <inheritdoc/>
        public override void OnCameraCleanup(CommandBuffer cmd) { }
    }
}
