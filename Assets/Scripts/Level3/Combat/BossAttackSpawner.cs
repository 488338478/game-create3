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

        [Header("Pool")]
        [SerializeField] private int totalPoolSize = 15;

        [Header("Boss Movement")]
        [Tooltip("Boss 跟随玩家的最大移动速度")]
        [SerializeField] private float followSpeed = 5f;
        [Tooltip("玩家开始移动后 Boss 延迟多久才跟（秒）")]
        [SerializeField] private float followDelay = 0.4f;

        [Header("Catch-up Warning")]
        [SerializeField] private SpriteRenderer bossSprite;
        [SerializeField] private float catchDistanceThreshold = 0.5f;
        [SerializeField] private float toRedSpeed = 8f;
        [SerializeField] private float toWhiteSpeed = 2f;

        private static readonly Color caughtColor = new(1f, 0.5f, 0.5f, 1f);

        [Header("Aiming")]
        [Tooltip("预判时间（秒），越大越提前瞄")]
        [SerializeField] private float predictionTime = 0.3f;
        [Tooltip("随机角度偏移范围（度）")]
        [SerializeField] private float aimSpreadAngle = 15f;

        [Header("Difficulty Scaling")]
        [SerializeField] private float spawnRateMultiplier = 1f;
        [SerializeField] private float maxSpawnRateMultiplier = 2.5f;
        [SerializeField] private float multiplierIncreasePerSecond = 0.02f;

        private SideScrollWorkspaceBase workspace;
        private readonly Queue<VerbalAttackProjectile> pool = new();
        private readonly HashSet<VerbalAttackProjectile> active = new();
        private bool isSpawning;
        private bool isStopped;

        private Vector2 lastPlayerPos;
        private Vector2 playerVelocity;
        private float followDelayTimer;
        private bool playerWasMoving;

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
            var pt = PlayerTransform;
            if (pt != null)
                lastPlayerPos = pt.position;
        }

        private void Update()
        {
            if (isStopped) return;

            var pt = PlayerTransform;
            if (pt != null)
            {
                Vector2 currentPos = pt.position;
                playerVelocity = (currentPos - lastPlayerPos) / Time.deltaTime;
                lastPlayerPos = currentPos;

                FollowPlayer(pt);
            }

            if (!isSpawning) return;
            if (spawnRateMultiplier < maxSpawnRateMultiplier)
                spawnRateMultiplier += multiplierIncreasePerSecond * Time.deltaTime;
        }

        private void FollowPlayer(Transform player)
        {
            var isMoving = playerVelocity.sqrMagnitude > 0.01f;

            if (isMoving && !playerWasMoving)
                followDelayTimer = followDelay;

            playerWasMoving = isMoving;

            if (followDelayTimer > 0f)
            {
                followDelayTimer -= Time.deltaTime;
                return;
            }

            var pos = transform.position;
            var targetX = player.position.x;
            pos.x = Mathf.MoveTowards(pos.x, targetX, followSpeed * Time.deltaTime);
            transform.position = pos;

            UpdateCatchColor(player);
        }

        private void UpdateCatchColor(Transform player)
        {
            if (bossSprite == null) return;

            var dist = Mathf.Abs(transform.position.x - player.position.x);
            var isCaught = dist <= catchDistanceThreshold;
            var targetColor = isCaught ? caughtColor : Color.white;
            var speed = isCaught ? toRedSpeed : toWhiteSpeed;

            bossSprite.color = Color.Lerp(bossSprite.color, targetColor, speed * Time.deltaTime);
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

        public void OnSequenceComplete()
        {
            StopAllSpawning();
            isStopped = true;
            if (bossSprite != null)
                bossSprite.color = Color.white;
        }

        // --- Pool ---

        public void ReturnProjectile(VerbalAttackProjectile proj)
        {
            if (proj == null) return;
            proj.gameObject.SetActive(false);
            active.Remove(proj);
            pool.Enqueue(proj);
        }

        private Transform poolRoot;

        private void PrewarmPool()
        {
            if (projectilePrefab == null) return;
            poolRoot = new GameObject("ProjectilePool").transform;
            for (var i = 0; i < totalPoolSize; i++)
            {
                var instance = Instantiate(projectilePrefab, poolRoot);
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
            while (isSpawning)
            {
                foreach (var wave in pattern.waves)
                {
                    if (!isSpawning) yield break;
                    if (wave.startTime > 0f)
                        yield return new WaitForSeconds(wave.startTime);

                    var waveEndTime = wave.duration > 0f ? Time.time + wave.duration : float.MaxValue;

                    while (isSpawning && Time.time < waveEndTime)
                    {
                        SpawnProjectile(wave);
                        yield return new WaitForSeconds(wave.spawnInterval / spawnRateMultiplier);
                    }
                }
            }
        }

        private void SpawnProjectile(BossAttackWave wave)
        {
            if (pool.Count == 0) return;

            var proj = pool.Dequeue();
            if (proj == null) return;

            var spawnPos = (Vector2)transform.position;
            var dir = CalculateAimDirection(spawnPos);

            proj.transform.position = spawnPos;
            proj.transform.localScale = Vector3.one * wave.spawnScale;
            proj.gameObject.SetActive(true);
            proj.InitFromPool(this, dir, wave.fallSpeed);
            active.Add(proj);
        }

        private Vector2 CalculateAimDirection(Vector2 from)
        {
            var pt = PlayerTransform;
            if (pt == null)
                return Vector2.down;

            Vector2 targetPos = (Vector2)pt.position + playerVelocity * predictionTime;
            var baseDir = (targetPos - from).normalized;

            var angle = Random.Range(-aimSpreadAngle, aimSpreadAngle);
            return RotateVector(baseDir, angle);
        }

        private static Vector2 RotateVector(Vector2 v, float degrees)
        {
            var rad = degrees * Mathf.Deg2Rad;
            var cos = Mathf.Cos(rad);
            var sin = Mathf.Sin(rad);
            return new Vector2(v.x * cos - v.y * sin, v.x * sin + v.y * cos);
        }
    }
}
