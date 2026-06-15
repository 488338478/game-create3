using System;
using UnityEngine;
using UnityEngine.Serialization;

namespace GameCreate3
{
    [Serializable]
    public struct PaletteColorOption
    {
        public int variantId;
        public string colorId;
        public string displayName;
        [HideInInspector]
        public Color fallbackColor;
        [FormerlySerializedAs("previewSprite")]
        public Sprite paletteSprite;

        public bool IsValid => variantId >= 0 || !string.IsNullOrWhiteSpace(colorId);

        public bool Matches(PaletteColorOption other)
        {
            return Matches(variantId, colorId, other.variantId, other.colorId);
        }

        public bool Matches(int otherVariantId, string otherColorId = null)
        {
            return Matches(variantId, colorId, otherVariantId, otherColorId);
        }

        public static bool Matches(int leftVariantId, string leftColorId, int rightVariantId, string rightColorId)
        {
            if (leftVariantId >= 0 && rightVariantId >= 0)
            {
                return leftVariantId == rightVariantId;
            }

            return string.Equals(Normalize(leftColorId), Normalize(rightColorId), StringComparison.OrdinalIgnoreCase);
        }

        public static bool Matches(string leftColorId, string rightColorId)
        {
            return Matches(-1, leftColorId, -1, rightColorId);
        }

        public static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }
    }
}
