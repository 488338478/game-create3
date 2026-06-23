using UnityEngine;

namespace GameCreate3
{
    [RequireComponent(typeof(SpriteRenderer))]
    public sealed class SpriteSwapAction : MonoBehaviour
    {
        [SerializeField] private Sprite alternateSprite;

        private SpriteRenderer sr;
        private Sprite originalSprite;
        private bool swapped;

        private void Awake()
        {
            sr = GetComponent<SpriteRenderer>();
            originalSprite = sr.sprite;
        }

        public void Swap()
        {
            if (alternateSprite == null) return;
            swapped = !swapped;
            sr.sprite = swapped ? alternateSprite : originalSprite;
        }

        public void SetAlternate()
        {
            if (alternateSprite == null) return;
            swapped = true;
            sr.sprite = alternateSprite;
        }

        public void SetOriginal()
        {
            swapped = false;
            sr.sprite = originalSprite;
        }
    }
}
