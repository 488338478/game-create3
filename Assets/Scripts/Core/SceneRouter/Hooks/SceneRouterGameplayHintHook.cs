using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace GameCreate3.Core.SceneRouting.Hooks
{
    public sealed class SceneRouterGameplayHintHook : MonoBehaviour
    {
        [Serializable]
        private sealed class SceneHintItem
        {
            [Header("Sprite")]
            [Tooltip("可选：提示图片。")]
            public Sprite sprite;

            [Tooltip("图片锚点位置，左下是 (0,0)，右上是 (1,1)。")]
            public Vector2 spriteNormalizedScreenPosition = new Vector2(0.5f, 0.18f);

            [Tooltip("图片额外像素偏移。")]
            public Vector2 spritePixelOffset = Vector2.zero;

            [Tooltip("图片最大显示尺寸。")]
            public Vector2 maxSpriteSize = new Vector2(420f, 180f);

            [Header("Text")]
            [TextArea(2, 5)]
            [Tooltip("可选：文字提示。留空则不显示文字。")]
            public string text;

            [Tooltip("可选：这条提示专用字体。不填则回退到组件默认字体。")]
            public TMP_FontAsset textFontAsset;

            [Tooltip("文字框锚点位置，左下是 (0,0)，右上是 (1,1)。")]
            public Vector2 textNormalizedScreenPosition = new Vector2(0.5f, 0.1f);

            [Tooltip("文字框额外像素偏移。")]
            public Vector2 textPixelOffset = Vector2.zero;

            [Tooltip("文字框尺寸。")]
            public Vector2 textSize = new Vector2(520f, 120f);

            [Tooltip("字号。启用自动缩放时作为最大字号。")]
            public float fontSize = 36f;

            [Tooltip("是否自动缩放字体。")]
            public bool enableAutoSizing = true;

            [Tooltip("自动缩放时的最小字号。")]
            public float minFontSize = 24f;

            [Tooltip("文字颜色。")]
            public Color textColor = Color.white;

            [Tooltip("文字对齐方式。")]
            public TextAlignmentOptions textAlignment = TextAlignmentOptions.Center;
        }

        [Serializable]
        private sealed class SceneHintEntry
        {
            [Tooltip("直接按场景名匹配，例如 Level1。")]
            public string sceneName;

            [Tooltip("按 SceneRouter routeId 匹配，优先级高于 Scene Name。")]
            public string routeId;

            [Tooltip("这个关卡要显示的提示条目。每条都能单独设置图、字、位置和尺寸。")]
            public List<SceneHintItem> hintItems = new List<SceneHintItem>();
        }

        private sealed class RuntimeHintItemView
        {
            public RectTransform root;
            public Image image;
            public TextMeshProUGUI text;
        }

        [Header("Hint Mapping")]
        [SerializeField] private List<SceneHintEntry> hintEntries = new List<SceneHintEntry>();

        [Header("Hide Condition")]
        [SerializeField] private float hideAfterKeyboardInputSeconds = 3f;
        [SerializeField] private bool useUnscaledTime = true;
        [SerializeField] private bool requireFreshKeyboardInputAfterShow = true;

        [Header("Canvas")]
        [SerializeField] private int sortingOrder = 200;

        [Header("Text Font")]
        [SerializeField] private TMP_FontAsset defaultTextFontAsset;

        private Canvas overlayCanvas;
        private RectTransform hintRoot;
        private readonly List<RuntimeHintItemView> runtimeViews = new List<RuntimeHintItemView>();
        private float keyboardInputElapsed;
        private bool isHintVisible;
        private bool waitingForFreshKeyboardInput;

        private void OnEnable()
        {
            SceneRouter.OnAfterChange += HandleSceneChanged;
        }

        private void Start()
        {
            RefreshForCurrentScene();
        }

        private void OnDisable()
        {
            SceneRouter.OnAfterChange -= HandleSceneChanged;
            DestroyRuntimeUi();
        }

        private void Update()
        {
            if (!isHintVisible)
            {
                return;
            }

            if (waitingForFreshKeyboardInput)
            {
                if (IsKeyboardInputActive())
                {
                    return;
                }

                waitingForFreshKeyboardInput = false;
                return;
            }

            if (!IsKeyboardInputActive())
            {
                return;
            }

            keyboardInputElapsed += useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
            if (keyboardInputElapsed >= hideAfterKeyboardInputSeconds)
            {
                HideHint();
            }
        }

        private void HandleSceneChanged(SceneRouteContext context)
        {
            RefreshHint(context.ToScene, context.ToRouteId);
        }

        private void RefreshForCurrentScene()
        {
            var activeScene = SceneManager.GetActiveScene();
            RefreshHint(activeScene.name, SceneRouter.CurrentRouteId);
        }

        private void RefreshHint(string sceneName, string routeId)
        {
            keyboardInputElapsed = 0f;

            var entry = FindMatchingEntry(sceneName, routeId);
            if (entry == null)
            {
                HideHint();
                return;
            }

            var items = GetValidItems(entry);
            if (items.Count == 0)
            {
                HideHint();
                return;
            }

            ShowHints(items);
        }

        private SceneHintEntry FindMatchingEntry(string sceneName, string routeId)
        {
            for (var i = 0; i < hintEntries.Count; i++)
            {
                var entry = hintEntries[i];
                if (entry == null || string.IsNullOrWhiteSpace(entry.routeId) || !HasAnyContent(entry))
                {
                    continue;
                }

                if (string.Equals(entry.routeId, routeId, StringComparison.OrdinalIgnoreCase))
                {
                    return entry;
                }
            }

            for (var i = 0; i < hintEntries.Count; i++)
            {
                var entry = hintEntries[i];
                if (entry == null || string.IsNullOrWhiteSpace(entry.sceneName) || !HasAnyContent(entry))
                {
                    continue;
                }

                if (string.Equals(entry.sceneName, sceneName, StringComparison.OrdinalIgnoreCase))
                {
                    return entry;
                }
            }

            return null;
        }

        private void ShowHints(IReadOnlyList<SceneHintItem> items)
        {
            EnsureRuntimeUi();

            for (var i = 0; i < items.Count; i++)
            {
                var view = GetOrCreateRuntimeView(i);
                ApplyHintItem(view, items[i]);
            }

            for (var i = items.Count; i < runtimeViews.Count; i++)
            {
                runtimeViews[i].root.gameObject.SetActive(false);
            }

            if (hintRoot != null)
            {
                hintRoot.gameObject.SetActive(true);
            }

            isHintVisible = true;
            waitingForFreshKeyboardInput = requireFreshKeyboardInputAfterShow;
        }

        private void HideHint()
        {
            if (hintRoot != null)
            {
                hintRoot.gameObject.SetActive(false);
            }

            isHintVisible = false;
            keyboardInputElapsed = 0f;
            waitingForFreshKeyboardInput = false;
        }

        private void EnsureRuntimeUi()
        {
            if (hintRoot != null)
            {
                return;
            }

            var canvasObject = new GameObject(
                "GameplayHintCanvas",
                typeof(RectTransform),
                typeof(Canvas),
                typeof(CanvasScaler),
                typeof(GraphicRaycaster));

            overlayCanvas = canvasObject.GetComponent<Canvas>();
            overlayCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            overlayCanvas.overrideSorting = true;
            overlayCanvas.sortingOrder = sortingOrder;
            overlayCanvas.pixelPerfect = false;

            var scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            var rootObject = new GameObject("GameplayHintRoot", typeof(RectTransform));
            rootObject.transform.SetParent(canvasObject.transform, false);

            hintRoot = rootObject.GetComponent<RectTransform>();
            hintRoot.anchorMin = Vector2.zero;
            hintRoot.anchorMax = Vector2.one;
            hintRoot.offsetMin = Vector2.zero;
            hintRoot.offsetMax = Vector2.zero;
            hintRoot.pivot = new Vector2(0.5f, 0.5f);
            hintRoot.gameObject.SetActive(false);
        }

        private RuntimeHintItemView GetOrCreateRuntimeView(int index)
        {
            while (runtimeViews.Count <= index)
            {
                runtimeViews.Add(CreateRuntimeView(runtimeViews.Count));
            }

            return runtimeViews[index];
        }

        private RuntimeHintItemView CreateRuntimeView(int index)
        {
            var rootObject = new GameObject($"GameplayHintItem_{index}", typeof(RectTransform));
            rootObject.transform.SetParent(hintRoot, false);

            var rootRect = rootObject.GetComponent<RectTransform>();
            rootRect.anchorMin = Vector2.zero;
            rootRect.anchorMax = Vector2.one;
            rootRect.offsetMin = Vector2.zero;
            rootRect.offsetMax = Vector2.zero;

            var imageObject = new GameObject("Sprite", typeof(RectTransform), typeof(Image));
            imageObject.transform.SetParent(rootObject.transform, false);
            var image = imageObject.GetComponent<Image>();
            image.preserveAspect = true;
            image.raycastTarget = false;
            imageObject.SetActive(false);

            var textObject = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
            textObject.transform.SetParent(rootObject.transform, false);
            var text = textObject.GetComponent<TextMeshProUGUI>();
            text.raycastTarget = false;
            text.enableWordWrapping = true;
            text.overflowMode = TextOverflowModes.Overflow;
            text.text = string.Empty;
            if (TMP_Settings.defaultFontAsset != null)
            {
                text.font = TMP_Settings.defaultFontAsset;
            }
            textObject.SetActive(false);

            return new RuntimeHintItemView
            {
                root = rootRect,
                image = image,
                text = text
            };
        }

        private void ApplyHintItem(RuntimeHintItemView view, SceneHintItem item)
        {
            var hasSprite = item != null && item.sprite != null;
            var hasText = item != null && !string.IsNullOrWhiteSpace(item.text);

            if (hasSprite)
            {
                ApplySprite(view.image, item);
            }

            view.image.gameObject.SetActive(hasSprite);

            if (hasText)
            {
                ApplyText(view.text, item, defaultTextFontAsset);
            }

            view.text.gameObject.SetActive(hasText);
            view.root.gameObject.SetActive(hasSprite || hasText);
        }

        private static void ApplyRect(RectTransform rectTransform, Vector2 normalizedPosition, Vector2 pixelOffset, Vector2 size)
        {
            rectTransform.anchorMin = normalizedPosition;
            rectTransform.anchorMax = normalizedPosition;
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
            rectTransform.anchoredPosition = pixelOffset;
            rectTransform.sizeDelta = size;
        }

        private static void ApplySprite(Image image, SceneHintItem item)
        {
            image.sprite = item.sprite;
            image.enabled = true;
            image.SetNativeSize();

            var nativeSize = image.rectTransform.sizeDelta;
            var clampedSize = ClampSize(nativeSize, item.maxSpriteSize);
            ApplyRect(image.rectTransform, item.spriteNormalizedScreenPosition, item.spritePixelOffset, clampedSize);
        }

        private static void ApplyText(TextMeshProUGUI text, SceneHintItem item, TMP_FontAsset defaultFontAsset)
        {
            text.text = item.text ?? string.Empty;
            var fontAsset = item.textFontAsset != null ? item.textFontAsset : defaultFontAsset;
            if (fontAsset != null)
            {
                text.font = fontAsset;
                if (fontAsset.material != null)
                {
                    text.fontSharedMaterial = fontAsset.material;
                }
            }
            text.color = item.textColor;
            text.alignment = item.textAlignment;
            text.fontSize = item.fontSize;
            text.enableAutoSizing = item.enableAutoSizing;
            text.fontSizeMin = Mathf.Min(item.minFontSize, item.fontSize);
            text.fontSizeMax = Mathf.Max(item.minFontSize, item.fontSize);
            ApplyRect(text.rectTransform, item.textNormalizedScreenPosition, item.textPixelOffset, item.textSize);
        }

        private static Vector2 ClampSize(Vector2 sourceSize, Vector2 maxSize)
        {
            if (sourceSize.x <= 0f || sourceSize.y <= 0f)
            {
                return sourceSize;
            }

            var widthScale = maxSize.x > 0f ? maxSize.x / sourceSize.x : 1f;
            var heightScale = maxSize.y > 0f ? maxSize.y / sourceSize.y : 1f;
            var scale = Mathf.Min(1f, widthScale, heightScale);
            return sourceSize * scale;
        }

        private void DestroyRuntimeUi()
        {
            if (overlayCanvas == null)
            {
                hintRoot = null;
                runtimeViews.Clear();
                return;
            }

            Destroy(overlayCanvas.gameObject);
            overlayCanvas = null;
            hintRoot = null;
            runtimeViews.Clear();
            isHintVisible = false;
            keyboardInputElapsed = 0f;
            waitingForFreshKeyboardInput = false;
        }

        private static bool HasAnyContent(SceneHintEntry entry)
        {
            if (entry == null || entry.hintItems == null)
            {
                return false;
            }

            for (var i = 0; i < entry.hintItems.Count; i++)
            {
                if (HasAnyContent(entry.hintItems[i]))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool HasAnyContent(SceneHintItem item)
        {
            return item != null && (item.sprite != null || !string.IsNullOrWhiteSpace(item.text));
        }

        private static List<SceneHintItem> GetValidItems(SceneHintEntry entry)
        {
            var items = new List<SceneHintItem>();
            if (entry == null || entry.hintItems == null)
            {
                return items;
            }

            for (var i = 0; i < entry.hintItems.Count; i++)
            {
                var item = entry.hintItems[i];
                if (HasAnyContent(item))
                {
                    items.Add(item);
                }
            }

            return items;
        }

        private static bool IsKeyboardInputActive()
        {
            if (Keyboard.current != null)
            {
                return Keyboard.current.anyKey.isPressed;
            }

#if ENABLE_LEGACY_INPUT_MANAGER
            return Input.anyKey;
#else
            return false;
#endif
        }
    }
}
