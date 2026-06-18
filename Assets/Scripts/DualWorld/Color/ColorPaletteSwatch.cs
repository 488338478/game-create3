using System;
using UnityEngine;
using UnityEngine.UI;

namespace GameCreate3
{
    public sealed class ColorPaletteSwatch : MonoBehaviour
    {
        [SerializeField] private Image swatchImage;
        [SerializeField] private Image selectionRing;
        [SerializeField] private GameObject lockOverlay;
        [SerializeField] private Text label;
        [SerializeField] private Sprite lockedSprite;
        [SerializeField] private string unlockedLabel = "梦境色";
        [SerializeField] private string lockedLabel = "未捕获";

        public PaletteColorOption Option { get; private set; }
        public bool HasColor { get; private set; }
        public int PaletteIndex { get; private set; } = -1;

        public void Configure(int paletteIndex, PaletteColorOption option, bool hasColor, bool selectable)
        {
            PaletteIndex = paletteIndex;
            Option = option;
            HasColor = hasColor;

            if (swatchImage != null)
            {
                swatchImage.sprite = hasColor
                    ? (option.paletteSprite != null ? option.paletteSprite : swatchImage.sprite)
                    : (lockedSprite != null ? lockedSprite : swatchImage.sprite);
                swatchImage.color = hasColor ? Color.white : new Color(0.22f, 0.25f, 0.31f, 0.85f);
                swatchImage.preserveAspect = true;
            }

            if (lockOverlay != null)
                lockOverlay.SetActive(!hasColor);

            if (label != null)
                label.text = hasColor
                    ? (string.IsNullOrWhiteSpace(option.displayName) ? unlockedLabel : option.displayName)
                    : lockedLabel;

            SetSelected(false);
            gameObject.SetActive(true);
        }

        public void SetSelected(bool selected)
        {
            if (selectionRing != null)
            {
                selectionRing.enabled = selected;
                selectionRing.color = selected
                    ? new Color(1f, 0.96f, 0.76f, 1f)
                    : new Color(1f, 1f, 1f, 0.12f);
            }
        }

        public void SetVisible(bool visible)
        {
            gameObject.SetActive(visible);
        }
    }
}
