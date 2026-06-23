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

        [Header("Projectile")]
        [SerializeField] private GameObject projectilePrefab;

        [Header("Spawn Area")]
        [SerializeField] private BoxCollider2D spawnZone;

        [Header("Pool")]
        [SerializeField] private int totalPoolSize = 15;

        [Header("Difficulty Scaling")]
        [SerializeField] private float spawnRateMultiplier = 1f;
        [SerializeField] private float maxSpawnRateMultiplier = 2.5f;
        [SerializeField] private float multiplierIncreasePerSecond = 0.02f;

        private SideScrollWorkspaceBase workspace;
        private readonly Queue<VerbalAttackProjectile> pool = new();
        private readonly HashSet<VerbalAttackProjectile> active = new();
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
            PrewarmPool();
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
            active.Remove(proj);
            pool.Enqueue(proj);
        }

        private void PrewarmPool()
        {
            if (projectilePrefab == null) return;
            for (var i = 0; i < totalPoolSize; i++)
            {
                var instance = Instantiate(projectilePrefab, transform);
                instance.SetActive(false);
                var proj = instance.GetComponent<VerbalAttackProjectile>();
                if (proj != null) pool.Enqueue(proj);
            }
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
            if (pool.Count == 0) return;

            var proj = pool.Dequeue();
            if (proj == null) return;

            var bounds = spawnZone.bounds;
            var pt = PlayerTransform;
            var sx = wave.spawnType switch
            {
                SpawnType.Targeted => pt != null ? pt.position.x : Random.Range(bounds.min.x, bounds.max.x),
                _ => Random.Range(bounds.min.x, bounds.max.x)
            };

            proj.transform.position = new Vector3(sx, bounds.max.y, 0f);
            proj.transform.localScale = Vector3.one * wave.spawnScale;
            proj.gameObject.SetActive(true);
            proj.InitFromPool(this, wave.fallSpeed, wave.swayAmplitude, wave.swayFrequency);
            active.Add(proj);
        }
    }
}
