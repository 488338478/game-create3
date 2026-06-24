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

        [Header("Spawn Rules")]
        [Tooltip("每 N 粉丝生成一个气泡")]
        [SerializeField] private int followersPerBubble = 100;
        [SerializeField] private int maxBubbles = 20;
        [SerializeField] private float spawnInterval = 0.05f;

        [Header("Bubble Animation")]
        [SerializeField] private float floatDistance = 60f;
        [SerializeField] private float lifetime = 1.2f;
        [SerializeField] private Color gainColor = new Color(1f, 0.4f, 0.5f, 1f);
        [SerializeField] private Color lossColor = new Color(0.5f, 0.5f, 0.5f, 1f);
        [SerializeField] private int fontSize = 24;

        private Canvas parentCanvas;

        private void Awake()
        {
            parentCanvas = GetComponentInParent<Canvas>(true);
        }

        public void SpawnBubbles(int delta)
        {
            if (delta == 0 || spawnCenter == null || bubblePrefab == null) return;
            StartCoroutine(SpawnSequence(delta));
        }

        private IEnumerator SpawnSequence(int delta)
        {
            var absDelta = Mathf.Abs(delta);
            var count = Mathf.Clamp(absDelta / followersPerBubble, 1, maxBubbles);
            var isGain = delta > 0;
            var perBubbleValue = absDelta / count;
            var prefix = isGain ? "+" : "-";

            for (int i = 0; i < count; i++)
            {
                var angle = Random.Range(0f, Mathf.PI * 2f);
                var offset = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * spawnRadius;

                var bubble = Instantiate(bubblePrefab, spawnCenter);
                var rt = bubble.GetComponent<RectTransform>();
                rt.anchoredPosition = offset;

                var text = bubble.GetComponentInChildren<Text>();
                if (text != null)
                {
                    text.text = $"{prefix}{perBubbleValue}";
                    text.color = isGain ? gainColor : lossColor;
                    text.fontSize = fontSize;
                }

                StartCoroutine(AnimateBubble(rt, text, offset));

                if (spawnInterval > 0f && i < count - 1)
                    yield return new WaitForSeconds(spawnInterval);
            }
        }

        private IEnumerator AnimateBubble(RectTransform rt, Text text, Vector2 startPos)
        {
            var elapsed = 0f;
            var startAlpha = text != null ? text.color.a : 1f;
            var direction = (startPos.normalized + Vector2.up).normalized;

            while (elapsed < lifetime)
            {
                elapsed += Time.deltaTime;
                var t = elapsed / lifetime;

                rt.anchoredPosition = startPos + direction * (floatDistance * t);

                if (text != null)
                {
                    var c = text.color;
                    c.a = startAlpha * (1f - t * t);
                    text.color = c;
                }

                var scale = 1f + 0.2f * Mathf.Sin(t * Mathf.PI);
                rt.localScale = Vector3.one * scale;

                yield return null;
            }

            Destroy(rt.gameObject);
        }
    }
}
