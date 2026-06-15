using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

namespace GameCreate3
{
    public sealed class DreamColorCollectController : MonoBehaviour
    {
        [Serializable]
        public sealed class MeteorDefinition
        {
            public PaletteColorOption option;
            public bool resonant = true;
            public float spawnWeight = 1f;
            public float fallSpeedMultiplier = 1f;
            public float horizontalDrift;
            public Sprite meteorSprite;
        }

        [Header("Legacy Static Pickups")]
        [SerializeField] private List<ColorCollectible> collectibles = new List<ColorCollectible>();

        [Header("Shared")]
        [SerializeField] private Transform playerTransform;
        [SerializeField] private float collectDistance = 0.5f;
        [SerializeField] private float colliderCollectPadding = 0.35f;
        [SerializeField] private bool autoCollect = true;
        [SerializeField] private int requiredCollectibleCount = 3;
        [SerializeField] private bool storeOnlyResonantColors = true;

        [Header("Meteor Rain")]
        [SerializeField] private bool useMeteorRain = true;
        [SerializeField] private DreamColorPickup meteorPrefab;
        [SerializeField] private Transform meteorContainer;
        [SerializeField] private List<Transform> meteorSpawnPoints = new List<Transform>();
        [SerializeField] private Vector2 fallbackSpawnXRange = new Vector2(-6f, 6f);
        [SerializeField] private float fallbackSpawnY = 4.5f;
        [SerializeField] private bool spawnFromCameraTopWhenNoSpawnPoints = true;
        [SerializeField] private Vector2 cameraViewportSpawnXRange = new Vector2(0.12f, 0.88f);
        [SerializeField] private float cameraTopMargin = 0.7f;
        [SerializeField] private bool spawnNearPlayerWhenNoSpawnPoints = true;
        [SerializeField] private Vector2 playerCenteredSpawnXRange = new Vector2(-3.25f, 3.25f);
        [SerializeField] private float playerCenteredSpawnY = 5f;
        [SerializeField] private Vector2 spawnIntervalRange = new Vector2(0.55f, 1.1f);
        [SerializeField] private int maxActiveMeteors = 4;
        [SerializeField] private float baseMeteorFallSpeed = 2.8f;
        [SerializeField] private float meteorFallAcceleration = 1.9f;
        [SerializeField] private float meteorGroundY = -3.2f;
        [SerializeField] private List<MeteorDefinition> meteorDefinitions = new List<MeteorDefinition>();

        [Header("Resonance")]
        [SerializeField] private List<DreamColorResonanceTarget> resonanceTargets = new List<DreamColorResonanceTarget>();
        [SerializeField] private float resonanceDuration = 0.45f;

        private bool interactive;
        private bool completed;
        private float nextMeteorSpawnTime;
        private List<PaletteColorOption> collectedOptions = new List<PaletteColorOption>();
        private List<ColorCollectible> activeCollectibles = new List<ColorCollectible>();
        private readonly List<DreamColorPickup> meteorPool = new List<DreamColorPickup>();
        private readonly List<DreamColorPickup> activeMeteors = new List<DreamColorPickup>();
        private readonly List<Collider2D> playerColliders = new List<Collider2D>();

        public event Action<IReadOnlyList<PaletteColorOption>> Completed;
        public event Action<PaletteColorOption> ItemCollected;

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
                var foundCollectibles = GetComponentsInChildren<ColorCollectible>(true);
                collectibles.AddRange(foundCollectibles);
            }

            if (meteorContainer == null)
            {
                meteorContainer = transform;
            }

            if (resonanceTargets.Count == 0)
            {
                var foundTargets = GetComponentsInChildren<DreamColorResonanceTarget>(true);
                resonanceTargets.AddRange(foundTargets);
            }

            meteorPool.Clear();
            meteorPool.AddRange(GetComponentsInChildren<DreamColorPickup>(true));
            foreach (var meteor in meteorPool)
            {
                if (meteor != null)
                {
                    meteor.ReturnToPool();
                }
            }

            ScheduleNextMeteorSpawn(true);
        }

        private void Update()
        {
            if (!interactive || completed)
            {
                return;
            }

            if (HasMeteorRainConfigured())
            {
                UpdateMeteorRain();
            }
            else if (autoCollect)
            {
                CheckCollection();
            }
        }

        private void CheckCollection()
        {
            if (playerTransform == null)
            {
                return;
            }

            for (int i = activeCollectibles.Count - 1; i >= 0; i--)
            {
                var collectible = activeCollectibles[i];
                if (collectible == null || collectible.IsCollected)
                {
                    continue;
                }

                if (IsWithinCollectRange(collectible.transform.position))
                {
                    CollectItem(collectible);
                }
            }

            // 检查是否完成
            if (ShouldAutoCompleteCollection() && collectedOptions.Count >= requiredCollectibleCount && !completed)
            {
                CompleteCollection();
            }
        }

        private void UpdateMeteorRain()
        {
            if (activeMeteors.Count < maxActiveMeteors && Time.time >= nextMeteorSpawnTime)
            {
                SpawnMeteor();
            }

            for (int i = activeMeteors.Count - 1; i >= 0; i--)
            {
                var meteor = activeMeteors[i];
                if (meteor == null)
                {
                    activeMeteors.RemoveAt(i);
                    continue;
                }

                var reachedGround = meteor.Tick(Time.deltaTime);
                if (reachedGround || !meteor.IsActive)
                {
                    activeMeteors.RemoveAt(i);
                }
            }

            if (autoCollect)
            {
                CheckMeteorCollection();
            }
        }

        private void CheckMeteorCollection()
        {
            if (playerTransform == null)
            {
                return;
            }

            for (int i = activeMeteors.Count - 1; i >= 0; i--)
            {
                var meteor = activeMeteors[i];
                if (meteor == null || !meteor.IsActive)
                {
                    activeMeteors.RemoveAt(i);
                    continue;
                }

                if (!IsWithinCollectRange(meteor.transform.position))
                {
                    continue;
                }

                ItemCollected?.Invoke(meteor.Option);

                if (meteor.IsResonant || !storeOnlyResonantColors)
                {
                    TryStoreCollectedOption(meteor.Option);
                }

                meteor.Collect();
                activeMeteors.RemoveAt(i);

                if (ShouldAutoCompleteCollection() && collectedOptions.Count >= requiredCollectibleCount && !completed)
                {
                    CompleteCollection();
                    return;
                }
            }
        }

        private void SpawnMeteor()
        {
            var pickup = AcquireMeteor();
            var definition = PickMeteorDefinition();
            if (pickup == null || definition == null)
            {
                ScheduleNextMeteorSpawn(false);
                return;
            }

            pickup.Activate(
                definition.option,
                definition.resonant,
                definition.meteorSprite,
                GetMeteorSpawnPosition(),
                baseMeteorFallSpeed * Mathf.Max(0.1f, definition.fallSpeedMultiplier),
                meteorFallAcceleration,
                0f,
                meteorGroundY);
            pickup.SetInteractive(interactive);
            activeMeteors.Add(pickup);

            if (definition.resonant)
            {
                TriggerResonance(definition.option);
            }

            ScheduleNextMeteorSpawn(false);
        }

        private DreamColorPickup AcquireMeteor()
        {
            for (int i = 0; i < meteorPool.Count; i++)
            {
                var meteor = meteorPool[i];
                if (meteor != null && !meteor.gameObject.activeSelf)
                {
                    return meteor;
                }
            }

            if (meteorPrefab == null)
            {
                var runtimeMeteor = CreateRuntimeMeteor();
                meteorPool.Add(runtimeMeteor);
                return runtimeMeteor;
            }

            var instance = Instantiate(meteorPrefab, meteorContainer != null ? meteorContainer : transform);
            instance.ReturnToPool();
            meteorPool.Add(instance);
            return instance;
        }

        private MeteorDefinition PickMeteorDefinition()
        {
            if (meteorDefinitions == null || meteorDefinitions.Count == 0)
            {
                return null;
            }

            float totalWeight = 0f;
            for (int i = 0; i < meteorDefinitions.Count; i++)
            {
                totalWeight += Mathf.Max(0.01f, meteorDefinitions[i].spawnWeight);
            }

            var pick = UnityEngine.Random.value * totalWeight;
            for (int i = 0; i < meteorDefinitions.Count; i++)
            {
                pick -= Mathf.Max(0.01f, meteorDefinitions[i].spawnWeight);
                if (pick <= 0f)
                {
                    return meteorDefinitions[i];
                }
            }

            return meteorDefinitions[meteorDefinitions.Count - 1];
        }

        private Vector3 GetMeteorSpawnPosition()
        {
            if (meteorSpawnPoints != null && meteorSpawnPoints.Count > 0)
            {
                var point = meteorSpawnPoints[UnityEngine.Random.Range(0, meteorSpawnPoints.Count)];
                if (point != null)
                {
                    return point.position;
                }
            }

            if (spawnFromCameraTopWhenNoSpawnPoints && TryGetCameraTopSpawnPosition(out var cameraSpawnPosition))
            {
                return cameraSpawnPosition;
            }

            var spawnOrigin = meteorContainer != null ? meteorContainer.position : transform.position;
            var spawnXRange = fallbackSpawnXRange;
            var spawnY = fallbackSpawnY;

            // 这关 prefabs 里还没挂显式刷点时，直接围绕玩家头顶刷，保证镜头内能看到落星。
            if (spawnNearPlayerWhenNoSpawnPoints && playerTransform != null)
            {
                spawnOrigin = playerTransform.position;
                spawnXRange = playerCenteredSpawnXRange;
                spawnY = playerCenteredSpawnY;
            }

            spawnOrigin.x += UnityEngine.Random.Range(spawnXRange.x, spawnXRange.y);
            spawnOrigin.y += spawnY;
            return spawnOrigin;
        }

        private bool TryGetCameraTopSpawnPosition(out Vector3 spawnPosition)
        {
            spawnPosition = default;

            var worldCamera = Camera.main;
            if (worldCamera == null)
            {
                return false;
            }

            var spawnPlaneZ = meteorContainer != null ? meteorContainer.position.z : transform.position.z;
            var cameraDistance = Mathf.Abs(worldCamera.transform.position.z - spawnPlaneZ);
            if (cameraDistance < 0.01f)
            {
                cameraDistance = Mathf.Abs(worldCamera.nearClipPlane) + 0.01f;
            }

            var leftHalfWidth = Mathf.Max(1f, Screen.width * 0.5f);
            var screenX = UnityEngine.Random.Range(
                leftHalfWidth * cameraViewportSpawnXRange.x,
                leftHalfWidth * cameraViewportSpawnXRange.y);
            var screenPoint = new Vector3(screenX, Screen.height, cameraDistance);
            spawnPosition = worldCamera.ScreenToWorldPoint(screenPoint);
            spawnPosition.y += cameraTopMargin;
            spawnPosition.z = spawnPlaneZ;
            return true;
        }

        private void TriggerResonance(PaletteColorOption option)
        {
            for (int i = 0; i < resonanceTargets.Count; i++)
            {
                if (resonanceTargets[i] != null)
                {
                    resonanceTargets[i].Pulse(option, resonanceDuration);
                }
            }
        }

        private bool HasMeteorRainConfigured()
        {
            return useMeteorRain &&
                   meteorDefinitions != null &&
                   meteorDefinitions.Count > 0;
        }

        private void ScheduleNextMeteorSpawn(bool immediate)
        {
            var minDelay = Mathf.Max(0.05f, spawnIntervalRange.x);
            var maxDelay = Mathf.Max(minDelay, spawnIntervalRange.y);
            var delay = immediate ? 0.05f : UnityEngine.Random.Range(minDelay, maxDelay);
            nextMeteorSpawnTime = Time.time + delay;
        }

        private void CollectItem(ColorCollectible collectible)
        {
            if (collectible == null)
            {
                return;
            }

            collectible.Collect();
            ItemCollected?.Invoke(collectible.Option);
            TryStoreCollectedOption(collectible.Option);
            activeCollectibles.Remove(collectible);
        }

        private void CompleteCollection()
        {
            completed = true;
            Completed?.Invoke(collectedOptions.AsReadOnly());
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

            for (int i = 0; i < activeMeteors.Count; i++)
            {
                if (activeMeteors[i] != null)
                {
                    activeMeteors[i].SetInteractive(enabled);
                }
            }

            if (enabled)
            {
                ScheduleNextMeteorSpawn(true);
                if (HasMeteorRainConfigured() && activeMeteors.Count == 0)
                {
                    SpawnMeteor();
                }
            }
        }

        public void ResetStage()
        {
            completed = false;
            interactive = false;
            collectedOptions.Clear();
            activeCollectibles.Clear();
            activeMeteors.Clear();

            foreach (var collectible in collectibles)
            {
                if (collectible != null)
                {
                    collectible.ResetCollectible();
                    activeCollectibles.Add(collectible);
                }
            }

            for (int i = 0; i < meteorPool.Count; i++)
            {
                if (meteorPool[i] != null)
                {
                    meteorPool[i].ReturnToPool();
                }
            }

            ScheduleNextMeteorSpawn(true);
        }

        public void SetPlayerTransform(Transform player)
        {
            playerTransform = player;
            RefreshPlayerColliders();
        }

        public void SetMeteorContainer(Transform container)
        {
            meteorContainer = container != null ? container : transform;
        }

        private void TryStoreCollectedOption(PaletteColorOption option)
        {
            if (option.IsValid)
            {
                for (var i = 0; i < collectedOptions.Count; i++)
                {
                    if (collectedOptions[i].Matches(option))
                    {
                        return;
                    }
                }
            }

            collectedOptions.Add(option);
        }

        private bool ShouldAutoCompleteCollection()
        {
            return requiredCollectibleCount > 0;
        }

        private bool IsWithinCollectRange(Vector3 targetPosition)
        {
            if (playerTransform == null)
            {
                return false;
            }

            if (playerColliders.Count == 0)
            {
                RefreshPlayerColliders();
            }

            var target = (Vector2)targetPosition;
            for (int i = 0; i < playerColliders.Count; i++)
            {
                var collider = playerColliders[i];
                if (collider == null || !collider.enabled)
                {
                    continue;
                }

                var closest = collider.ClosestPoint(target);
                if (Vector2.Distance(closest, target) <= colliderCollectPadding)
                {
                    return true;
                }
            }

            return Vector2.Distance(playerTransform.position, target) <= collectDistance;
        }

        private void RefreshPlayerColliders()
        {
            playerColliders.Clear();

            if (playerTransform == null)
            {
                return;
            }

            playerColliders.AddRange(playerTransform.GetComponentsInChildren<Collider2D>(true));
        }

        private DreamColorPickup CreateRuntimeMeteor()
        {
            var root = new GameObject("RuntimeDreamMeteor");
            root.transform.SetParent(meteorContainer != null ? meteorContainer : transform, false);

            var visual = new GameObject("Visual");
            visual.transform.SetParent(root.transform, false);

            var spriteRenderer = visual.AddComponent<SpriteRenderer>();
            spriteRenderer.sortingOrder = 24;

            var collider = visual.AddComponent<CircleCollider2D>();
            collider.radius = 0.35f;

            var pickup = root.AddComponent<DreamColorPickup>();
            pickup.ReturnToPool();
            return pickup;
        }
    }

    public sealed class ColorCollectible : MonoBehaviour
    {
        [SerializeField] private int variantId = -1;
        [SerializeField] private string colorId = string.Empty;
        [SerializeField] private string displayName = string.Empty;
        [SerializeField] private Color color = Color.white;
        [FormerlySerializedAs("previewSprite")]
        [SerializeField] private Sprite paletteSprite;
        [SerializeField] private SpriteRenderer spriteRenderer;
        [SerializeField] private Collider2D collectibleCollider;
        [SerializeField] private GameObject visualObject;

        public PaletteColorOption Option => new PaletteColorOption
        {
            variantId = variantId,
            colorId = colorId,
            displayName = displayName,
            fallbackColor = color,
            paletteSprite = paletteSprite != null ? paletteSprite : (spriteRenderer != null ? spriteRenderer.sprite : null)
        };
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
                spriteRenderer.sprite = paletteSprite != null ? paletteSprite : spriteRenderer.sprite;
                spriteRenderer.color = paletteSprite != null ? Color.white : color;
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
