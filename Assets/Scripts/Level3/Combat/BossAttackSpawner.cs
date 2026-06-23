using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GameCreate3.Level3
{
    public sealed class BossAttackSpawner : MonoBehaviour
    {
        [Header("Attack Patterns")]
        [SerializeField] private BossAttackPattern phase1Pattern;
        [SerializeField] private BossAttackPattern phase2Pattern;

        [Header("Spawn Area")]
        [SerializeField] private BoxCollider2D spawnZone;

        [Header("Pool")]
        [SerializeField] private int poolSizePerPrefab = 15;

        [Header("Difficulty Scaling")]
        [SerializeField] private float spawnRateMultiplier = 1f;
        [SerializeField] private float maxSpawnRateMultiplier = 2.5f;
        [SerializeField] private float multiplierIncreasePerSecond = 0.02f;

        private SideScrollWorkspaceBase workspace;
        private readonly Dictionary<GameObject, Queue<VerbalAttackProjectile>> pools = new();
        private readonly Dictionary<VerbalAttackProjectile, Queue<VerbalAttackProjectile>> activeToPool = new();
        private bool isSpawning;

        private Transform PlayerTransform =>
            workspace != null && workspace.PlayerController != null
                ? workspace.PlayerController.transform
                : null;

        private void Awake()
        {
            workspace = GetComponentInParent<SideScrollWorkspaceBase>(true);
        }

        private void Start()
        {
            PrewarmPools();
        }

        private void Update()
        {
            if (!isSpawning) return;
            if (spawnRateMultiplier < maxSpawnRateMultiplier)
                spawnRateMultiplier += multiplierIncreasePerSecond * Time.deltaTime;
        }

        // --- WorkspaceEventRouter 调用的 public 入口 ---

        public void OnPhase1()
        {
            StopAllSpawning();
            StartSpawning(phase1Pattern);
        }

        public void OnPhase2()
        {
            StopAllSpawning();
            spawnRateMultiplier = Mathf.Max(spawnRateMultiplier, 1.2f);
            StartSpawning(phase2Pattern);
        }

        public void OnPhase3()
        {
            StopAllSpawning();
        }

        // --- Pool ---

        public void ReturnProjectile(VerbalAttackProjectile proj)
        {
            if (proj == null) return;
            proj.gameObject.SetActive(false);
            if (activeToPool.TryGetValue(proj, out var queue))
            {
                queue.Enqueue(proj);
                activeToPool.Remove(proj);
            }
        }

        private void PrewarmPools()
        {
            var allPrefabs = new HashSet<GameObject>();
            CollectPrefabs(phase1Pattern, allPrefabs);
            CollectPrefabs(phase2Pattern, allPrefabs);

            foreach (var prefab in allPrefabs)
            {
                if (prefab == null) continue;
                var queue = new Queue<VerbalAttackProjectile>();
                for (var i = 0; i < poolSizePerPrefab; i++)
                {
                    var instance = Instantiate(prefab, transform);
                    instance.SetActive(false);
                    var proj = instance.GetComponent<VerbalAttackProjectile>();
                    if (proj != null) queue.Enqueue(proj);
                }
                pools[prefab] = queue;
            }
        }

        private static void CollectPrefabs(BossAttackPattern pattern, HashSet<GameObject> set)
        {
            if (pattern == null) return;
            foreach (var wave in pattern.waves)
                if (wave.projectilePrefabs != null)
                    foreach (var p in wave.projectilePrefabs)
                        if (p != null) set.Add(p);
        }

        private void StartSpawning(BossAttackPattern pattern)
        {
            if (pattern == null || pattern.waves.Count == 0) return;
            isSpawning = true;
            StartCoroutine(RunWaves(pattern));
        }

        private void StopAllSpawning()
        {
            isSpawning = false;
            StopAllCoroutines();
        }

        private IEnumerator RunWaves(BossAttackPattern pattern)
        {
            foreach (var wave in pattern.waves)
            {
                if (!isSpawning) yield break;
                if (wave.startTime > 0f)
                    yield return new WaitForSeconds(wave.startTime);

                var waveEndTime = wave.duration > 0f ? Time.time + wave.duration : float.MaxValue;
                var totalSpawned = 0;

                while (isSpawning && Time.time < waveEndTime && totalSpawned < wave.maxProjectiles)
                {
                    SpawnProjectile(wave);
                    totalSpawned++;
                    yield return new WaitForSeconds(wave.spawnInterval / spawnRateMultiplier);
                }
            }
        }

        private void SpawnProjectile(BossAttackWave wave)
        {
            if (wave.projectilePrefabs == null || wave.projectilePrefabs.Count == 0) return;

            var prefab = wave.projectilePrefabs[Random.Range(0, wave.projectilePrefabs.Count)];
            if (prefab == null) return;
            if (!pools.TryGetValue(prefab, out var queue) || queue.Count == 0) return;

            var proj = queue.Dequeue();
            if (proj == null) return;

            var bounds = spawnZone.bounds;
            var pt = PlayerTransform;
            var sx = wave.spawnType switch
            {
                SpawnType.Targeted => pt != null ? pt.position.x : Random.Range(bounds.min.x, bounds.max.x),
                _ => Random.Range(bounds.min.x, bounds.max.x)
            };

            proj.transform.position = new Vector3(sx, bounds.max.y, 0f);
            proj.gameObject.SetActive(true);
            proj.InitFromPool(this, wave.fallSpeed, wave.swayAmplitude, wave.swayFrequency);
            activeToPool[proj] = queue;
        }
    }
}
