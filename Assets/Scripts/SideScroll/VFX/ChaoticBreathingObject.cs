using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

namespace GameCreate3.SideScroll.VFX
{
    /// <summary>
    /// 让物体整体产生亮暗呼吸和轻微的非规律位置漂移。
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("GameCreate3/SideScroll/VFX/Chaotic Breathing Object")]
    public sealed class ChaoticBreathingObject : MonoBehaviour
    {
        private static readonly int ColorId = Shader.PropertyToID("_Color");
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int FlashId = Shader.PropertyToID("_Flash");

        [Header("作用范围")]
        [Tooltip("为空时使用当前物体。位移会作用在这个 Transform 上。")]
        [SerializeField] private Transform targetRoot;

        [Tooltip("是否影响子物体上的 Renderer / UI Graphic。")]
        [SerializeField] private bool includeChildren = true;

        [Tooltip("使用不受 Time.timeScale 影响的时间。暂停时仍想呼吸就打开。")]
        [SerializeField] private bool useUnscaledTime;

        [Header("明暗呼吸")]
        [SerializeField] private bool animateBrightness = true;

        [Tooltip("完整亮-暗-亮循环的速度。")]
        [Min(0.01f)]
        [SerializeField] private float brightnessSpeed = 0.75f;

        [Tooltip("最暗时的颜色倍率。1 表示不变。")]
        [Range(0f, 2f)]
        [SerializeField] private float darkMultiplier = 0.65f;

        [Tooltip("最亮时的提亮强度。1 表示不变，2 表示尽量往白色提亮。")]
        [Range(0f, 2f)]
        [SerializeField] private float brightMultiplier = 1.15f;

        [Tooltip("呼吸曲线。横轴 0-1 表示一个循环，纵轴 0=最暗，1=最亮。")]
        [SerializeField] private AnimationCurve brightnessCurve = new AnimationCurve(
            new Keyframe(0f, 1f),
            new Keyframe(0.5f, 0f),
            new Keyframe(1f, 1f)
        );

        [Header("2D 光照增强")]
        [Tooltip("可选。给物体或子物体挂一个 Light2D 后拖进来，可以让纯白/高亮素材也明显变亮。为空时会自动找子物体里的 Light2D。")]
        [SerializeField] private Light2D breathingLight;

        [SerializeField] private bool animateLightIntensity = true;

        [Tooltip("最暗时 Light2D 的强度倍率。")]
        [Min(0f)]
        [SerializeField] private float lightDarkMultiplier = 0.7f;

        [Tooltip("最亮时 Light2D 的强度倍率。")]
        [Min(0f)]
        [SerializeField] private float lightBrightMultiplier = 1.8f;

        [Header("位置呼吸")]
        [SerializeField] private bool animatePosition = true;

        [Tooltip("本地坐标上的最大漂移幅度。")]
        [SerializeField] private Vector3 positionAmplitude = new Vector3(0.03f, 0.05f, 0f);

        [Tooltip("漂移整体速度。")]
        [Min(0.01f)]
        [SerializeField] private float positionSpeed = 0.8f;

        [Tooltip("混沌感。0 更接近周期摆动，1 更像不规则游走。")]
        [Range(0f, 1f)]
        [SerializeField] private float chaosAmount = 0.55f;

        [Tooltip("位置跟随平滑时间。越大越柔。")]
        [Min(0f)]
        [SerializeField] private float positionSmoothTime = 0.18f;

        [Tooltip("启动时随机相位，避免多个物体同步呼吸。")]
        [SerializeField] private bool randomizePhaseOnEnable = true;

        private Transform Target => targetRoot != null ? targetRoot : transform;

        private Vector3 baseLocalPosition;
        private Vector3 positionVelocity;
        private float timeOffset;

        private SpriteRenderer[] spriteRenderers = System.Array.Empty<SpriteRenderer>();
        private Color[] spriteBaseColors = System.Array.Empty<Color>();

        private Graphic[] graphics = System.Array.Empty<Graphic>();
        private Color[] graphicBaseColors = System.Array.Empty<Color>();

        private Renderer[] meshRenderers = System.Array.Empty<Renderer>();
        private Color[] rendererBaseColors = System.Array.Empty<Color>();
        private MaterialPropertyBlock propertyBlock;
        private float baseLightIntensity;

        private void Awake()
        {
            if (targetRoot == null)
            {
                targetRoot = transform;
            }

            propertyBlock = new MaterialPropertyBlock();
            if (breathingLight == null)
            {
                breathingLight = GetComponentInChildren<Light2D>(true);
            }
            CacheTargets();
        }

        private void OnEnable()
        {
            baseLocalPosition = Target.localPosition;
            positionVelocity = Vector3.zero;

            if (randomizePhaseOnEnable)
            {
                timeOffset = Random.Range(0f, 1000f);
            }

            if (breathingLight != null)
            {
                baseLightIntensity = breathingLight.intensity;
            }
        }

        private void OnDisable()
        {
            RestoreBrightness();
            Target.localPosition = baseLocalPosition;
        }

        private void LateUpdate()
        {
            float now = (useUnscaledTime ? Time.unscaledTime : Time.time) + timeOffset;
            float deltaTime = useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;

            if (animateBrightness)
            {
                ApplyBrightness(EvaluateBrightness(now));
            }

            if (animatePosition)
            {
                ApplyPosition(now, deltaTime);
            }
        }

        [ContextMenu("Refresh Render Targets")]
        public void CacheTargets()
        {
            var root = Target;
            spriteRenderers = includeChildren
                ? root.GetComponentsInChildren<SpriteRenderer>(true)
                : root.GetComponents<SpriteRenderer>();
            spriteBaseColors = new Color[spriteRenderers.Length];
            for (int i = 0; i < spriteRenderers.Length; i++)
            {
                spriteBaseColors[i] = spriteRenderers[i].color;
            }

            graphics = includeChildren
                ? root.GetComponentsInChildren<Graphic>(true)
                : root.GetComponents<Graphic>();
            graphicBaseColors = new Color[graphics.Length];
            for (int i = 0; i < graphics.Length; i++)
            {
                graphicBaseColors[i] = graphics[i].color;
            }

            var allRenderers = includeChildren
                ? root.GetComponentsInChildren<Renderer>(true)
                : root.GetComponents<Renderer>();
            meshRenderers = FilterNonSpriteRenderers(allRenderers);
            rendererBaseColors = new Color[meshRenderers.Length];
            for (int i = 0; i < meshRenderers.Length; i++)
            {
                rendererBaseColors[i] = ReadRendererColor(meshRenderers[i]);
            }
        }

        private float EvaluateBrightness(float now)
        {
            float cycle = Mathf.Repeat(now * brightnessSpeed, 1f);
            float curveValue = Mathf.Clamp01(brightnessCurve.Evaluate(cycle));
            return Mathf.Lerp(darkMultiplier, brightMultiplier, curveValue);
        }

        private void ApplyBrightness(float multiplier)
        {
            // multiplier <= 1 darkens the vertex color; multiplier > 1 drives a
            // shader "flash" that lifts the texture pixels toward white, so a
            // full-white sprite can still visibly brighten without bloom/HDR.
            float flash = Mathf.Clamp01(multiplier - 1f);
            for (int i = 0; i < spriteRenderers.Length; i++)
            {
                var sprite = spriteRenderers[i];
                if (sprite == null) continue;
                sprite.color = ApplyBrightnessToColor(spriteBaseColors[i], multiplier);
                sprite.GetPropertyBlock(propertyBlock);
                propertyBlock.SetFloat(FlashId, flash);
                sprite.SetPropertyBlock(propertyBlock);
            }

            for (int i = 0; i < graphics.Length; i++)
            {
                if (graphics[i] == null) continue;
                graphics[i].color = ApplyBrightnessToColor(graphicBaseColors[i], multiplier);
            }

            for (int i = 0; i < meshRenderers.Length; i++)
            {
                var rendererTarget = meshRenderers[i];
                if (rendererTarget == null) continue;

                rendererTarget.GetPropertyBlock(propertyBlock);
                Color color = ApplyBrightnessToColor(rendererBaseColors[i], multiplier);
                propertyBlock.SetColor(ColorId, color);
                propertyBlock.SetColor(BaseColorId, color);
                rendererTarget.SetPropertyBlock(propertyBlock);
            }

            if (animateLightIntensity && breathingLight != null)
            {
                breathingLight.intensity = baseLightIntensity * EvaluateLightMultiplier(multiplier);
            }
        }

        private void RestoreBrightness()
        {
            for (int i = 0; i < spriteRenderers.Length; i++)
            {
                var sprite = spriteRenderers[i];
                if (sprite == null) continue;
                sprite.color = spriteBaseColors[i];
                sprite.GetPropertyBlock(propertyBlock);
                propertyBlock.SetFloat(FlashId, 0f);
                sprite.SetPropertyBlock(propertyBlock);
            }

            for (int i = 0; i < graphics.Length; i++)
            {
                if (graphics[i] != null) graphics[i].color = graphicBaseColors[i];
            }

            for (int i = 0; i < meshRenderers.Length; i++)
            {
                var rendererTarget = meshRenderers[i];
                if (rendererTarget == null) continue;

                rendererTarget.GetPropertyBlock(propertyBlock);
                propertyBlock.SetColor(ColorId, rendererBaseColors[i]);
                propertyBlock.SetColor(BaseColorId, rendererBaseColors[i]);
                rendererTarget.SetPropertyBlock(propertyBlock);
            }

            if (breathingLight != null)
            {
                breathingLight.intensity = baseLightIntensity;
            }
        }

        private float EvaluateLightMultiplier(float colorMultiplier)
        {
            float range = brightMultiplier - darkMultiplier;
            float normalized = Mathf.Abs(range) > 0.0001f
                ? Mathf.InverseLerp(darkMultiplier, brightMultiplier, colorMultiplier)
                : 1f;

            return Mathf.Lerp(lightDarkMultiplier, lightBrightMultiplier, normalized);
        }

        private void ApplyPosition(float now, float deltaTime)
        {
            float t = now * positionSpeed;
            var pendulum = new Vector3(
                Mathf.Sin(t * 1.17f) + 0.5f * Mathf.Sin(t * 2.31f + 1.7f),
                Mathf.Sin(t * 0.91f + 2.4f) + 0.5f * Mathf.Sin(t * 1.83f + 0.2f),
                Mathf.Sin(t * 1.07f + 4.1f) + 0.5f * Mathf.Sin(t * 2.07f + 2.9f)
            ) / 1.5f;

            var noise = new Vector3(
                PerlinSigned(t, 13.1f),
                PerlinSigned(t, 37.7f),
                PerlinSigned(t, 71.3f)
            );

            Vector3 mixed = Vector3.Lerp(pendulum, noise, chaosAmount);
            Vector3 targetPosition = baseLocalPosition + Vector3.Scale(positionAmplitude, mixed);

            if (positionSmoothTime <= 0f)
            {
                Target.localPosition = targetPosition;
                return;
            }

            Target.localPosition = Vector3.SmoothDamp(
                Target.localPosition,
                targetPosition,
                ref positionVelocity,
                positionSmoothTime,
                Mathf.Infinity,
                deltaTime
            );
        }

        private static Renderer[] FilterNonSpriteRenderers(Renderer[] renderers)
        {
            int count = 0;
            for (int i = 0; i < renderers.Length; i++)
            {
                if (!(renderers[i] is SpriteRenderer)) count++;
            }

            var filtered = new Renderer[count];
            int index = 0;
            for (int i = 0; i < renderers.Length; i++)
            {
                if (renderers[i] is SpriteRenderer) continue;
                filtered[index++] = renderers[i];
            }

            return filtered;
        }

        private static Color ReadRendererColor(Renderer rendererTarget)
        {
            var sharedMaterial = rendererTarget.sharedMaterial;
            if (sharedMaterial != null && sharedMaterial.HasProperty(ColorId))
            {
                return sharedMaterial.GetColor(ColorId);
            }

            if (sharedMaterial != null && sharedMaterial.HasProperty(BaseColorId))
            {
                return sharedMaterial.GetColor(BaseColorId);
            }

            return Color.white;
        }

        private static float PerlinSigned(float t, float seed)
        {
            return Mathf.PerlinNoise(seed, t) * 2f - 1f;
        }

        private static Color ApplyBrightnessToColor(Color color, float multiplier)
        {
            if (multiplier <= 1f)
            {
                color.r *= multiplier;
                color.g *= multiplier;
                color.b *= multiplier;
                return color;
            }

            float lift = Mathf.Clamp01(multiplier - 1f);
            color.r = Mathf.Lerp(color.r, 1f, lift);
            color.g = Mathf.Lerp(color.g, 1f, lift);
            color.b = Mathf.Lerp(color.b, 1f, lift);
            return color;
        }

        private void OnValidate()
        {
            brightnessSpeed = Mathf.Max(0.01f, brightnessSpeed);
            positionSpeed = Mathf.Max(0.01f, positionSpeed);
            positionSmoothTime = Mathf.Max(0f, positionSmoothTime);

            if (brightnessCurve == null || brightnessCurve.length == 0)
            {
                brightnessCurve = new AnimationCurve(
                    new Keyframe(0f, 1f),
                    new Keyframe(0.5f, 0f),
                    new Keyframe(1f, 1f)
                );
            }
        }
    }
}
