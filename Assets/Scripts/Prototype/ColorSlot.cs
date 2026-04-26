using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace GameCreate3
{
    public sealed class ColorSlot : MonoBehaviour
    {
        [SerializeField] private Image colorImage;
        [SerializeField] private Button colorButton;
        [SerializeField] private Color targetColor;
        [SerializeField] private float colorTolerance = 0.01f;
        [SerializeField] private List<Color> availableColors = new List<Color>();

        private int currentColorIndex;
        private bool dreamPaletteEnabled;
        private bool interactable = true;

        public void Initialize(Image image, Button button, Color target)
        {
            colorImage = image;
            colorButton = button;
            targetColor = target;

            if (colorButton != null)
            {
                colorButton.onClick.AddListener(CycleColor);
            }

            UpdateColorDisplay();
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

        public void SetDreamPaletteEnabled(bool enabled)
        {
            dreamPaletteEnabled = enabled;
            if (enabled && availableColors.Count > 0)
            {
                currentColorIndex = 0;
                UpdateColorDisplay();
            }
        }

        public void SetAvailableColors(List<Color> colors)
        {
            availableColors.Clear();
            if (colors != null)
            {
                availableColors.AddRange(colors);
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
            if (colorImage == null)
            {
                return false;
            }

            var currentColor = colorImage.color;
            return Mathf.Abs(currentColor.r - targetColor.r) < colorTolerance &&
                   Mathf.Abs(currentColor.g - targetColor.g) < colorTolerance &&
                   Mathf.Abs(currentColor.b - targetColor.b) < colorTolerance &&
                   Mathf.Abs(currentColor.a - targetColor.a) < colorTolerance;
        }

        public void ResetSlot()
        {
            currentColorIndex = 0;
            availableColors.Clear();
            UpdateColorDisplay();
        }

        private void CycleColor()
        {
            if (!interactable)
            {
                return;
            }

            if (dreamPaletteEnabled && availableColors.Count > 0)
            {
                currentColorIndex = (currentColorIndex + 1) % availableColors.Count;
            }
            else
            {
                // 如果没有梦境色卡，使用默认颜色循环
                currentColorIndex = (currentColorIndex + 1) % 8;
            }

            UpdateColorDisplay();
        }

        private void UpdateColorDisplay()
        {
            if (colorImage == null)
            {
                return;
            }

            if (dreamPaletteEnabled && availableColors.Count > 0)
            {
                colorImage.color = availableColors[currentColorIndex];
            }
            else
            {
                // 默认颜色
                colorImage.color = GetDefaultColor(currentColorIndex);
            }
        }

        private Color GetDefaultColor(int index)
        {
            switch (index)
            {
                case 0: return Color.white;
                case 1: return Color.red;
                case 2: return Color.green;
                case 3: return Color.blue;
                case 4: return Color.yellow;
                case 5: return Color.cyan;
                case 6: return Color.magenta;
                case 7: return Color.black;
                default: return Color.white;
            }
        }
    }
}
