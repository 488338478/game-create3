using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace GameCreate3.Level3
{
    public sealed class FollowerBubbleSpawner : MonoBehaviour
    {
        [Header("Spawn Circle")]
        [SerializeField] private RectTransform spawnCenter;
        [SerializeField] private float spawnRadius = 120f;

        [Header("Bubble Prefab")]
        [SerializeField] private GameObject bubblePrefab;

        [Header("Images")]
        [SerializeField] private Sprite[] gainSprites;
        [SerializeField] private Sprite[] lossSprites;
        [SerializeField] private Sprite[] autoSprites;
        [SerializeField] private int lossAmountPerBubble = 200;

        [Header("Animation")]
        [SerializeField] private float floatDistance = 80f;
        [SerializeField] private float lifetime = 1.2f;

        private bool autoActive;
        private float autoTimer;

        public void SetAutoActive(bool active) => autoActive = active;

        private void Update()
        {
            if (!autoActive || autoSprites == null || autoSprites.Length == 0) return;

            autoTimer += Time.deltaTime;
            if (autoTimer >= 1f)
            {
                autoTimer -= 1f;
                SpawnOne(autoSprites);
            }
        }

        public void SpawnBubbles(int delta)
        {
            if (delta == 0) return;
            var sprites = delta > 0 ? gainSprites : lossSprites;
            var spawnCount = 1;

            if (delta < 0 && lossAmountPerBubble > 0)
                spawnCount = Mathf.Max(1, Mathf.RoundToInt(Mathf.Abs(delta) / (float)lossAmountPerBubble));

            for (var i = 0; i < spawnCount; i++)
                SpawnOne(sprites);
        }

        private void SpawnOne(Sprite[] sprites)
        {
            if (spawnCenter == null || bubblePrefab == null) return;

            var angle = Random.Range(0f, Mathf.PI * 2f);
            var offset = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * spawnRadius;

            var bubble = Instantiate(bubblePrefab, spawnCenter);
            var rt = bubble.GetComponent<RectTransform>();
            rt.anchoredPosition = offset;
            rt.localScale = Vector3.one;

            var img = bubble.GetComponent<Image>();
            if (img != null && sprites != null && sprites.Length > 0)
            {
                img.sprite = sprites[Random.Range(0, sprites.Length)];
                img.SetNativeSize();
            }

            StartCoroutine(AnimateBubble(rt, img, offset));
        }

        private IEnumerator AnimateBubble(RectTransform rt, Image img, Vector2 startPos)
        {
            var elapsed = 0f;

            while (elapsed < lifetime)
            {
                elapsed += Time.deltaTime;
                var t = elapsed / lifetime;

                rt.anchoredPosition = startPos + Vector2.up * (floatDistance * t);

                if (img != null)
                {
                    var c = img.color;
                    c.a = 1f - t * t;
                    img.color = c;
                }

                rt.localScale = Vector3.one * (1f + 0.15f * Mathf.Sin(t * Mathf.PI));

                yield return null;
            }

            Destroy(rt.gameObject);
        }
    }
}
