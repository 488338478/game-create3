using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace GameCreate3
{
    public sealed class ColorSlot : MonoBehaviour, IPointerClickHandler
    {
        [SerializeField] private Image colorImage;
        [SerializeField] private Button colorButton;
        [SerializeField] private int blockIndex = -1;
        [SerializeField] private int targetVariantId = -1;
        [SerializeField] private string targetColorId = string.Empty;
        [SerializeField] private Color targetColor;
        [SerializeField] private float colorTolerance = 0.01f;
        [SerializeField] private List<PaletteColorOption> availableOptions = new List<PaletteColorOption>();
        [SerializeField] private List<ColorApplyTarget> applyTargets = new List<ColorApplyTarget>();
        [SerializeField] private bool usePaletteSelectionWhenDreamEnabled = true;

        private bool dreamPaletteEnabled;
        private bool interactable = true;
        private PaletteColorOption currentOption;
        private Sprite baseDisplaySprite;
        private Color baseDisplayColor = Color.white;

        public event System.Action<ColorSlot> Clicked;
        public event System.Action<ColorSlot> StateChanged;

        public Color CurrentColor => colorImage != null ? colorImage.color : currentOption.fallbackColor;
        public string CurrentColorId => currentOption.colorId;

        public void Initialize(Image image, Button button, Color target)
        {
            colorImage = image;
            colorButton = button;
            targetColor = target;
            CacheBaseDisplay();

            if (colorButton != null)
            {
                colorButton.onClick.AddListener(CycleColor);
            }

            RestoreBaseDisplay();
        }

        private void Awake()
        {
            if (colorButton == null)
            {
                colorButton = GetComponent<Button>();
            }
            if (colorImage == null)
            {
                colorImage = GetComponent<Image>();
            }

            CacheBaseDisplay();

            if (applyTargets.Count == 0)
            {
                var foundTargets = GetComponentsInChildren<ColorApplyTarget>(true);
                applyTargets.AddRange(foundTargets);
            }

            RestoreBaseDisplay();
        }

        private void OnEnable()
        {
            if (colorButton != null)
            {
                colorButton.onClick.RemoveListener(CycleColor);
                colorButton.onClick.AddListener(CycleColor);
            }
        }

        private void OnDisable()
        {
            if (colorButton != null)
            {
                colorButton.onClick.RemoveListener(CycleColor);
            }
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (colorButton != null)
            {
                return;
            }

            CycleColor();
        }

        public void SetDreamPaletteEnabled(bool enabled)
        {
            dreamPaletteEnabled = enabled;
        }

        public void SetAvailableColors(IReadOnlyList<PaletteColorOption> options)
        {
            availableOptions.Clear();
            if (options != null)
            {
                for (var i = 0; i < options.Count; i++)
                {
                    availableOptions.Add(options[i]);
                }
            }
        }

        public void SetInteractable(bool enabled)
        {
            interactable = enabled;
            if (colorButton != null)
            {
                colorButton.interactable = enabled;
            }
        }

        public bool IsCorrectColor()
        {
            if (targetVariantId >= 0 || !string.IsNullOrWhiteSpace(targetColorId) || currentOption.variantId >= 0 || !string.IsNullOrWhiteSpace(currentOption.colorId))
            {
                return currentOption.Matches(targetVariantId, targetColorId);
            }

            var currentColor = CurrentColor;
            return Mathf.Abs(currentColor.r - targetColor.r) < colorTolerance &&
                   Mathf.Abs(currentColor.g - targetColor.g) < colorTolerance &&
                   Mathf.Abs(currentColor.b - targetColor.b) < colorTolerance &&
                   Mathf.Abs(currentColor.a - targetColor.a) < colorTolerance;
        }

        public void ApplyPaletteColor(PaletteColorOption option)
        {
            ApplyOption(ResolveLocalOption(option));
        }

        public bool MatchesOption(PaletteColorOption option)
        {
            return option.IsValid && option.Matches(targetVariantId, targetColorId);
        }

        public bool TryGetBlockIndex(out int resolvedBlockIndex)
        {
            // blockIndex 是独立的空间映射字段，请在 Inspector 中显式配置。
            // 不再从 targetVariantId 推导，避免颜色身份编号与空间位置编号耦合。
            if (blockIndex >= 0)
            {
                resolvedBlockIndex = blockIndex;
                return true;
            }

            // 兜底：从 GameObject 名 "Target_N" 解析
            if (TryParseBlockIndexFromName(gameObject.name, out resolvedBlockIndex))
            {
                return true;
            }

            resolvedBlockIndex = -1;
            return false;
        }

        private List<ColorApplyTarget> ResolveApplyTargets()
        {
            if (applyTargets == null) applyTargets = new List<ColorApplyTarget>();
            if (applyTargets.Count == 0 || applyTargets.TrueForAll(t => t == null))
            {
                applyTargets.Clear();
                var onSelf = GetComponent<ColorApplyTarget>();
                if (onSelf != null) applyTargets.Add(onSelf);
                applyTargets.AddRange(GetComponentsInChildren<ColorApplyTarget>(true));
            }
            return applyTargets;
        }

        public void PlayHintPulse(PaletteColorOption option)
        {
            // 已经涂成正确颜色的色槽，不再提示闪烁
            if (IsCorrectColor())
            {
                return;
            }

            var resolvedOption = ResolveLocalOption(option);
            var targets = ResolveApplyTargets();
            for (var i = 0; i < targets.Count; i++)
            {
                if (targets[i] != null)
                {
                    targets[i].PlayHintColorPulse(resolvedOption);
                }
            }
        }

        public void StopApplyTargetPulses()
        {
            var targets = ResolveApplyTargets();
            for (var i = 0; i < targets.Count; i++)
            {
                if (targets[i] != null)
                {
                    targets[i].StopHintColorPulse();
                }
            }
        }

        private Coroutine hintPulseRoutine;
        private static readonly Color HintPulseWhite = new Color(1f, 1f, 1f, 1f);

        private System.Collections.IEnumerator HintPulseRoutine(PaletteColorOption option)
        {
            if (colorImage == null) yield break;

            var baseSprite = colorImage.sprite;
            var baseColor = colorImage.color;

            if (option.paletteSprite != null)
            {
                colorImage.sprite = option.paletteSprite;
                colorImage.preserveAspect = true;
                colorImage.color = Color.white;
            }
            else if (option.fallbackColor.a > 0f)
            {
                colorImage.color = option.fallbackColor;
            }

            var targetColor = colorImage.color;
            var elapsed = 0f;
            const float duration = 0.22f;

            while (true)
            {
                elapsed += Time.deltaTime;
                var normalized = (elapsed % duration) / duration;
                var wave = Mathf.Sin(normalized * Mathf.PI);
                colorImage.color = Color.Lerp(targetColor, HintPulseWhite, wave * 0.35f);
                yield return null;
            }
        }

        public void StopHintPulse()
        {
            if (hintPulseRoutine != null)
            {
                StopCoroutine(hintPulseRoutine);
                hintPulseRoutine = null;
            }
        }

        public void ResetSlot()
        {
            StopHintPulse();
            currentOption = default;
            RestoreBaseDisplay();

            for (var i = 0; i < applyTargets.Count; i++)
            {
                if (applyTargets[i] != null)
                {
                    applyTargets[i].ResetTarget();
                }
            }

            StateChanged?.Invoke(this);
        }

        private void CycleColor()
        {
            if (!interactable)
            {
                return;
            }

            if (!dreamPaletteEnabled)
            {
                return;
            }

            if (!usePaletteSelectionWhenDreamEnabled)
            {
                return;
            }

            Clicked?.Invoke(this);
        }

        private void ApplyOption(PaletteColorOption option)
        {
            StopHintPulse();
            currentOption = option;
            var hasApplyTargets = applyTargets != null && applyTargets.Count > 0;

            if (!option.IsValid && option.paletteSprite == null)
            {
                if (!hasApplyTargets)
                {
                    RestoreBaseDisplay();
                }

                for (var i = 0; i < applyTargets.Count; i++)
                {
                    if (applyTargets[i] != null)
                    {
                        applyTargets[i].ResetTarget();
                    }
                }

                StateChanged?.Invoke(this);

                return;
            }

            if (!hasApplyTargets && colorImage != null)
            {
                colorImage.sprite = option.paletteSprite != null ? option.paletteSprite : baseDisplaySprite;
                colorImage.color = option.paletteSprite != null
                    ? Color.white
                    : (option.fallbackColor.a > 0f ? option.fallbackColor : baseDisplayColor);
                colorImage.preserveAspect = option.paletteSprite != null;
            }

            var isCorrect = IsCorrectColor();

            for (var i = 0; i < applyTargets.Count; i++)
            {
                if (applyTargets[i] != null)
                {
                    applyTargets[i].ApplyVariant(option, isCorrect);
                }
            }

            StateChanged?.Invoke(this);
        }

        private PaletteColorOption ResolveLocalOption(PaletteColorOption option)
        {
            if (!option.IsValid || availableOptions == null || availableOptions.Count == 0)
            {
                return option;
            }

            for (var i = 0; i < availableOptions.Count; i++)
            {
                if (availableOptions[i].Matches(option))
                {
                    return availableOptions[i];
                }
            }

            return option;
        }

        private void CacheBaseDisplay()
        {
            if (colorImage == null)
            {
                return;
            }

            baseDisplaySprite = colorImage.sprite;
            baseDisplayColor = colorImage.color;
        }

        private void RestoreBaseDisplay()
        {
            if (colorImage == null)
            {
                return;
            }

            colorImage.sprite = baseDisplaySprite;
            colorImage.color = baseDisplayColor;
            colorImage.preserveAspect = baseDisplaySprite != null;
        }

        private static bool TryParseBlockIndexFromName(string value, out int parsedIndex)
        {
            parsedIndex = -1;

            if (string.IsNullOrWhiteSpace(value))
            {
                return false;
            }

            const string prefix = "Target_";
            var marker = value.IndexOf(prefix, System.StringComparison.OrdinalIgnoreCase);
            if (marker < 0)
            {
                return false;
            }

            var start = marker + prefix.Length;
            var end = start;
            while (end < value.Length && char.IsDigit(value[end]))
            {
                end++;
            }

            if (end <= start)
            {
                return false;
            }

            return int.TryParse(
                value.Substring(start, end - start),
                NumberStyles.Integer,
                CultureInfo.InvariantCulture,
                out parsedIndex);
        }
    }
}
