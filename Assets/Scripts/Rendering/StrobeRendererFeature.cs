using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace GameCreate3.Rendering
{
    /// <summary>
    /// URP 14 ScriptableRendererFeature — 全屏频闪/雪花/RGB偏移/扫描线/撕裂 后处理。
    /// 默认注入点：AfterRendering（最末端，UI 也会被影响）。
    /// 必须在 Renderer2DData 上手动添加该 Feature。详见 README_StrobeEffect.md。
    /// </summary>
    public sealed class StrobeRendererFeature : ScriptableRendererFeature
    {
        [Tooltip("强制使用的 shader。留空则按名 'Game/PostProcess/Strobe2D' 查找。")]
        [SerializeField] private Shader strobeShader;

        [Tooltip("渲染注入点。AfterRenderingTransparents 保证 source 是 RT 而非 backbuffer。")]
        [SerializeField] private RenderPassEvent injectionPoint = RenderPassEvent.AfterRenderingTransparents;

        private Material material;
        private StrobePass pass;

        /// <summary>当前激活的实例（Controller 通过它访问 material 与开关）。</summary>
        public static StrobeRendererFeature Instance { get; private set; }

        /// <summary>当前是否参与渲染。Controller 在 Play/Stop 时设置。</summary>
        public bool IsActive { get; set; }

        /// <summary>共享 material — Controller 通过它写参数。</summary>
        public Material Material => material;

        public override void Create()
        {
            Instance = this;

            if (strobeShader == null)
            {
                strobeShader = Shader.Find("Game/PostProcess/Strobe2D");
            }

            if (strobeShader == null)
            {
                Debug.LogError("[StrobeRendererFeature] Shader 'Game/PostProcess/Strobe2D' 未找到，请确认 Assets/Shaders/Strobe2D.shader 存在并已编译。");
                return;
            }

            material = CoreUtils.CreateEngineMaterial(strobeShader);
            pass = new StrobePass(material) { renderPassEvent = injectionPoint };
        }

        public override void SetupRenderPasses(ScriptableRenderer renderer, in RenderingData renderingData)
        {
            if (pass == null) return;
            pass.SetSource(renderer.cameraColorTargetHandle);
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            if (!IsActive || material == null || pass == null) return;
            if (renderingData.cameraData.cameraType != CameraType.Game) return;

            renderer.EnqueuePass(pass);
        }

        protected override void Dispose(bool disposing)
        {
            pass?.Dispose();
            pass = null;

            if (material != null)
            {
                CoreUtils.Destroy(material);
                material = null;
            }

            if (Instance == this) Instance = null;
        }

        // ---------------------------------------------------------------------

        private sealed class StrobePass : ScriptableRenderPass
        {
            private readonly Material material;
            private RTHandle source;
            private RTHandle tempTarget;

            public StrobePass(Material material)
            {
                this.material = material;
            }

            public void SetSource(RTHandle cameraColor)
            {
                source = cameraColor;
            }

            public override void Configure(CommandBuffer cmd, RenderTextureDescriptor cameraTextureDescriptor)
            {
                var desc = cameraTextureDescriptor;
                desc.depthBufferBits = 0;
                desc.msaaSamples = 1;
                RenderingUtils.ReAllocateIfNeeded(
                    ref tempTarget,
                    desc,
                    FilterMode.Bilinear,
                    TextureWrapMode.Clamp,
                    name: "_StrobeTempRT");
            }

            public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
            {
                if (material == null || source == null || tempTarget == null) return;

                var cmd = CommandBufferPool.Get("StrobeEffect");
                // material 读 source 写 temp（避免同 RTHandle 读写）；再 copy 回 source。
                Blitter.BlitCameraTexture(cmd, source, tempTarget, material, 0);
                Blitter.BlitCameraTexture(cmd, tempTarget, source);
                context.ExecuteCommandBuffer(cmd);
                CommandBufferPool.Release(cmd);
            }

            public void Dispose()
            {
                tempTarget?.Release();
                tempTarget = null;
            }
        }
    }
}
