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
            if (blockIndex >= 0)
            {
                resolvedBlockIndex = blockIndex;
                return true;
            }

            if (targetVariantId > 0)
            {
                resolvedBlockIndex = targetVariantId - 1;
                return true;
            }

            if (TryParseBlockIndexFromName(gameObject.name, out resolvedBlockIndex))
            {
                return true;
            }

            resolvedBlockIndex = -1;
            return false;
        }

        public void PlayHintPulse(PaletteColorOption option)
        {
            var resolvedOption = ResolveLocalOption(option);
            for (var i = 0; i < applyTargets.Count; i++)
            {
                if (applyTargets[i] != null)
                {
                    applyTargets[i].PlayHintPulse(resolvedOption);
                }
            }
        }

        public void ResetSlot()
        {
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

            if (dreamPaletteEnabled && usePaletteSelectionWhenDreamEnabled)
            {
                Clicked?.Invoke(this);
            }
        }

        private void ApplyOption(PaletteColorOption option)
        {
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
