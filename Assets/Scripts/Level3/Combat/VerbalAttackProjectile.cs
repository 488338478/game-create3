using UnityEngine;

namespace GameCreate3.Level3
{
    [RequireComponent(typeof(Collider2D))]
    [RequireComponent(typeof(Rigidbody2D))]
    public sealed class VerbalAttackProjectile : MonoBehaviour
    {
        [Header("Sprites")]
        [SerializeField] private Sprite[] sprites;

        [Header("Deflect")]
        [SerializeField] private float deflectSpeed = 8f;
        [SerializeField] private Color deflectTint = Color.cyan;
        [SerializeField] private float hitStopDuration = 0.2f;

        [Header("HitStop VFX")]
        [SerializeField] private float shakeIntensity = 0.08f;
        [SerializeField] private float flashInterval = 0.03f;

        [Header("Damage")]
        [SerializeField] private int damage = 1;
        [SerializeField] private LayerMask destroyOnContact;

        public bool IsDeflected { get; private set; }

        private SpriteRenderer spriteRenderer;
        private Rigidbody2D rb;
        private BossAttackSpawner pool;
        private Vector2 direction;
        private float speed;
        private float deflectTimer;

        private float hitStopTimer;
        private Vector3 hitStopAnchor;
        private float flashTimer;

        private const float SafeDespawnDistance = 15f;

        private void Awake()
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
            rb = GetComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Kinematic;
            rb.useFullKinematicContacts = true;

            var col = GetComponent<Collider2D>();
            col.isTrigger = true;

            destroyOnContact = (1 << 3) | (1 << 8);
        }

        public void InitFromPool(BossAttackSpawner owner, Vector2 dir, float spd)
        {
            pool = owner;
            direction = dir.normalized;
            speed = spd;
            IsDeflected = false;
            hitStopTimer = 0f;

            if (spriteRenderer != null)
                spriteRenderer.color = Color.white;

            RandomizeSprite();
        }

        public void Deflect(Vector2 deflectDirection)
        {
            IsDeflected = true;
            direction = deflectDirection.normalized;
            speed = deflectSpeed;
            deflectTimer = 2f;
            hitStopTimer = hitStopDuration;
            hitStopAnchor = transform.position;
            flashTimer = 0f;
            if (spriteRenderer != null)
                spriteRenderer.color = Color.white;
        }

        public void ReturnToPool()
        {
            if (pool != null)
                pool.ReturnProjectile(this);
            else
                gameObject.SetActive(false);
        }

        private void FixedUpdate()
        {
            if (hitStopTimer > 0f)
            {
                hitStopTimer -= Time.fixedDeltaTime;

                // Shake
                var offset = (Vector3)(Random.insideUnitCircle * shakeIntensity);
                transform.position = hitStopAnchor + offset;

                // Flash white/tint
                if (spriteRenderer != null)
                {
                    flashTimer -= Time.fixedDeltaTime;
                    if (flashTimer <= 0f)
                    {
                        flashTimer = flashInterval;
                        spriteRenderer.color = spriteRenderer.color == Color.white ? deflectTint : Color.white;
                    }
                }

                if (hitStopTimer <= 0f)
                {
                    transform.position = hitStopAnchor;
                    if (spriteRenderer != null)
                        spriteRenderer.color = deflectTint;
                }
                return;
            }

            if (IsDeflected)
            {
                deflectTimer -= Time.fixedDeltaTime;
                if (deflectTimer <= 0f)
                {
                    ReturnToPool();
                    return;
                }
            }

            var pos = rb.position;
            pos += direction * (speed * Time.fixedDeltaTime);

            if (pos.magnitude > SafeDespawnDistance)
            {
                ReturnToPool();
                return;
            }

            rb.MovePosition(pos);
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (IsDeflected) return;

            var otherLayer = 1 << other.gameObject.layer;
            if ((destroyOnContact.value & otherLayer) == 0) return;

            var combatState = other.GetComponentInParent<PlayerCombatState>(true);
            if (combatState != null)
                combatState.OnProjectileHit(damage);

            ReturnToPool();
        }

        private void RandomizeSprite()
        {
            if (sprites != null && sprites.Length > 0 && spriteRenderer != null)
                spriteRenderer.sprite = sprites[Random.Range(0, sprites.Length)];
        }
    }
}
