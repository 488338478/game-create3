using UnityEngine;

namespace GameCreate3.Level3
{
    [RequireComponent(typeof(Collider2D))]
    public sealed class VerbalAttackProjectile : MonoBehaviour
    {
        [Header("Movement")]
        [SerializeField] private float fallSpeed = 3f;
        [SerializeField] private float swayAmplitude;
        [SerializeField] private float swayFrequency = 1f;

        [Header("Deflect")]
        [SerializeField] private float deflectSpeed = 8f;
        [SerializeField] private Color deflectTint = Color.cyan;

        [Header("Damage")]
        [SerializeField] private int damage = 1;
        [SerializeField] private LayerMask playerLayer;

        public bool IsDeflected { get; private set; }

        private SpriteRenderer spriteRenderer;
        private BossAttackSpawner pool;
        private float elapsed;

        private void Awake()
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
            var col = GetComponent<Collider2D>();
            col.isTrigger = true;
        }

        public void InitFromPool(BossAttackSpawner owner, float speed, float amplitude, float frequency)
        {
            pool = owner;
            fallSpeed = speed;
            swayAmplitude = amplitude;
            swayFrequency = frequency;
            elapsed = 0f;
            IsDeflected = false;

            if (spriteRenderer != null)
                spriteRenderer.color = Color.white;
        }

        public void Deflect()
        {
            IsDeflected = true;
            if (spriteRenderer != null)
                spriteRenderer.color = deflectTint;
        }

        public void ReturnToPool()
        {
            if (pool != null)
                pool.ReturnProjectile(this);
            else
                gameObject.SetActive(false);
        }

        private void Update()
        {
            if (IsDeflected)
            {
                transform.position += Vector3.up * (deflectSpeed * Time.deltaTime);
                if (transform.position.y > 10f)
                    ReturnToPool();
            }
            else
            {
                elapsed += Time.deltaTime;
                var prevSway = swayAmplitude * Mathf.Sin((elapsed - Time.deltaTime) * swayFrequency * Mathf.PI * 2f);
                var currSway = swayAmplitude * Mathf.Sin(elapsed * swayFrequency * Mathf.PI * 2f);
                transform.position += new Vector3(currSway - prevSway, -fallSpeed * Time.deltaTime, 0f);

                if (transform.position.y < -8f)
                    ReturnToPool();
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (IsDeflected) return;

            if (playerLayer == 0 || (playerLayer.value & (1 << other.gameObject.layer)) != 0)
            {
                var combatState = other.GetComponentInParent<PlayerCombatState>(true);
                if (combatState != null)
                {
                    combatState.TakeDamage(damage);
                    ReturnToPool();
                }
            }
        }
    }
}
