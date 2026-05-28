using System.Collections;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

namespace GameCreate3
{
    /// <summary>
    /// 触发后逐渐让屏幕变全白。
    /// 双层效果：Global Light 2D 过曝（场景层）+ Canvas 白色遮罩（UI 层）。
    /// 任意一层留空则只用另一层。
    /// 调用 <see cref="Trigger"/> 开始，<see cref="Reverse"/> 反向淡出。
    /// </summary>
    public sealed class ScreenWhiteout : MonoBehaviour
    {
        [Header("Global Light 2D（场景过曝）")]
        [Tooltip("留空则自动在场景中查找第一个 Global 类型的 Light2D")]
        [SerializeField] private Light2D globalLight;
        [Tooltip("过曝目标强度，原始强度会在 Reverse 时还原")]
        [SerializeField] private float targetLightIntensity = 4f;

        [Header("Canvas 白色遮罩（全屏覆盖）")]
        [Tooltip("留空则运行时自动创建")]
        [SerializeField] private Image overlayImage;
        [Tooltip("遮罩所在 Canvas 的 Sort Order")]
        [SerializeField] private int overlaySortOrder = 999;

        [Header("时间")]
        [SerializeField] private float duration = 0.8f;
        [Tooltip("控制淡入曲线，默认线性。X=时间0-1，Y=强度0-1")]
        [SerializeField] private AnimationCurve curve = AnimationCurve.Linear(0, 0, 1, 1);

        private float originalLightIntensity;
        private Coroutine activeRoutine;
        private GameObject generatedOverlayRoot;

        private void Awake()
        {
            EnsureOverlay();
            EnsureGlobalLight();

            if (globalLight != null)
                originalLightIntensity = globalLight.intensity;

            SetOverlayAlpha(0f);
        }

        private void OnDestroy()
        {
            if (generatedOverlayRoot != null)
            {
                Destroy(generatedOverlayRoot);
                generatedOverlayRoot = null;
            }
        }

        /// <summary>开始变白，可挂在 UnityEvent 上与 SineMover.Trigger 同时调用。</summary>
        public void Trigger() => Run(forward: true);

        /// <summary>反向淡出，恢复原始状态。</summary>
        public void Reverse() => Run(forward: false);

        private void Run(bool forward)
        {
            if (activeRoutine != null) StopCoroutine(activeRoutine);
            activeRoutine = StartCoroutine(WhiteoutRoutine(forward));
        }

        private IEnumerator WhiteoutRoutine(bool forward)
        {
            float elapsed = 0f;
            float safeDuration = Mathf.Max(0.01f, duration);

            while (elapsed < safeDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / safeDuration);
                float value = curve.Evaluate(forward ? t : 1f - t);
                Apply(value);
                yield return null;
            }

            Apply(forward ? 1f : 0f);
            activeRoutine = null;
        }

        private void Apply(float value)
        {
            // Canvas 遮罩
            SetOverlayAlpha(value);

            // Global Light 过曝
            if (globalLight != null)
                globalLight.intensity = Mathf.Lerp(originalLightIntensity, targetLightIntensity, value);
        }

        private void SetOverlayAlpha(float alpha)
        {
            if (overlayImage == null) return;
            var c = overlayImage.color;
            c.a = alpha;
            overlayImage.color = c;
        }

        // ── 自动创建 ──────────────────────────────────────────────

        private void EnsureOverlay()
        {
            if (overlayImage != null) return;

            var go     = new GameObject("ScreenWhiteoutOverlay");
            generatedOverlayRoot = go;
            var canvas = go.AddComponent<Canvas>();
            canvas.renderMode  = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = overlaySortOrder;
            go.AddComponent<CanvasScaler>();
            go.AddComponent<GraphicRaycaster>();

            var imgGo  = new GameObject("White");
            imgGo.transform.SetParent(go.transform, false);
            var rect   = imgGo.AddComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            overlayImage       = imgGo.AddComponent<Image>();
            overlayImage.color = new Color(1f, 1f, 1f, 0f);
            overlayImage.raycastTarget = false;
        }

        private void EnsureGlobalLight()
        {
            if (globalLight != null) return;
            foreach (var light in FindObjectsOfType<Light2D>())
            {
                if (light.lightType == Light2D.LightType.Global)
                {
                    globalLight = light;
                    break;
                }
            }
        }
    }
}
