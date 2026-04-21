using System;
using System.Collections.Generic;
using UnityEngine;

namespace GameCreate3
{
    public sealed class DreamColorCollectController : MonoBehaviour
    {
        [SerializeField] private List<ColorCollectible> collectibles = new List<ColorCollectible>();
        [SerializeField] private Transform playerTransform;
        [SerializeField] private float collectDistance = 0.5f;
        [SerializeField] private bool autoCollect = true;
        [SerializeField] private int requiredCollectibleCount = 3;

        private bool interactive;
        private bool completed;
        private List<Color> collectedColors = new List<Color>();
        private List<ColorCollectible> activeCollectibles = new List<ColorCollectible>();

        public event Action<IReadOnlyList<Color>> Completed;

        public void Initialize(List<ColorCollectible> collectibleList, Transform player)
        {
            collectibles = collectibleList ?? new List<ColorCollectible>();
            playerTransform = player;
            activeCollectibles.Clear();
            activeCollectibles.AddRange(collectibles);
        }

        private void Awake()
        {
            if (collectibles.Count == 0)
            {
                // 自动查找子物体中的 ColorCollectible
                var foundCollectibles = GetComponentsInChildren<ColorCollectible>();
                collectibles.AddRange(foundCollectibles);
            }
        }

        private void Update()
        {
            if (!interactive || completed || !autoCollect)
            {
                return;
            }

            CheckCollection();
        }

        private void CheckCollection()
        {
            if (playerTransform == null)
            {
                return;
            }

            var playerPosition = playerTransform.position;

            for (int i = activeCollectibles.Count - 1; i >= 0; i--)
            {
                var collectible = activeCollectibles[i];
                if (collectible == null || collectible.IsCollected)
                {
                    continue;
                }

                var distance = Vector2.Distance(playerPosition, collectible.transform.position);
                if (distance <= collectDistance)
                {
                    CollectItem(collectible);
                }
            }

            // 检查是否完成
            if (collectedColors.Count >= requiredCollectibleCount && !completed)
            {
                CompleteCollection();
            }
        }

        private void CollectItem(ColorCollectible collectible)
        {
            if (collectible == null)
            {
                return;
            }

            collectible.Collect();
            collectedColors.Add(collectible.Color);
            activeCollectibles.Remove(collectible);
        }

        private void CompleteCollection()
        {
            completed = true;
            Completed?.Invoke(collectedColors.AsReadOnly());
        }

        public void SetInteractive(bool enabled)
        {
            interactive = enabled;

            foreach (var collectible in collectibles)
            {
                if (collectible != null)
                {
                    collectible.SetInteractive(enabled);
                }
            }
        }

        public void ResetStage()
        {
            completed = false;
            interactive = false;
            collectedColors.Clear();
            activeCollectibles.Clear();

            foreach (var collectible in collectibles)
            {
                if (collectible != null)
                {
                    collectible.ResetCollectible();
                    activeCollectibles.Add(collectible);
                }
            }
        }

        public void SetPlayerTransform(Transform player)
        {
            playerTransform = player;
        }
    }

    public sealed class ColorCollectible : MonoBehaviour
    {
        [SerializeField] private Color color = Color.white;
        [SerializeField] private SpriteRenderer spriteRenderer;
        [SerializeField] private Collider2D collectibleCollider;
        [SerializeField] private GameObject visualObject;

        public Color Color => color;
        public bool IsCollected { get; private set; }

        public void Initialize(Color collectibleColor, SpriteRenderer renderer)
        {
            color = collectibleColor;
            spriteRenderer = renderer;
            UpdateVisual();
        }

        private void Awake()
        {
            if (spriteRenderer == null)
            {
                spriteRenderer = GetComponent<SpriteRenderer>();
            }
            if (collectibleCollider == null)
            {
                collectibleCollider = GetComponent<Collider2D>();
            }
            if (visualObject == null)
            {
                visualObject = gameObject;
            }

            UpdateVisual();
        }

        private void UpdateVisual()
        {
            if (spriteRenderer != null)
            {
                spriteRenderer.color = color;
            }
        }

        public void Collect()
        {
            if (IsCollected)
            {
                return;
            }

            IsCollected = true;

            if (visualObject != null)
            {
                visualObject.SetActive(false);
            }

            if (collectibleCollider != null)
            {
                collectibleCollider.enabled = false;
            }
        }

        public void ResetCollectible()
        {
            IsCollected = false;

            if (visualObject != null)
            {
                visualObject.SetActive(true);
            }

            if (collectibleCollider != null)
            {
                collectibleCollider.enabled = true;
            }
        }

        public void SetInteractive(bool enabled)
        {
            if (collectibleCollider != null)
            {
                collectibleCollider.enabled = enabled && !IsCollected;
            }
        }
    }
}
