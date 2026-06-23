using UnityEngine;

namespace GameCreate3.Level3
{
    [RequireComponent(typeof(Collider2D))]
    [RequireComponent(typeof(Rigidbody2D))]
    public sealed class VerbalAttackProjectile : MonoBehaviour
    {
        [Header("Sprites")]
        [SerializeField] private Sprite[] sprites;

        [Header("Movement")]
        [SerializeField] private float fallSpeed = 3f;
        [SerializeField] private float swayAmplitude;
        [SerializeField] private float swayFrequency = 1f;

        [Header("Deflect")]
        [SerializeField] private float deflectSpeed = 8f;
        [SerializeField] private Color deflectTint = Color.cyan;

        [Header("Damage")]
        [SerializeField] private int damage = 1;
        [SerializeField] private LayerMask destroyOnContact;

        public bool IsDeflected { get; private set; }

        private SpriteRenderer spriteRenderer;
        private Rigidbody2D rb;
        private BossAttackSpawner pool;
        private float elapsed;

        private const float SafeDespawnDistance = 30f;

        private void Awake()
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
            rb = GetComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Kinematic;
            rb.useFullKinematicContacts = true;

            var col = GetComponent<Collider2D>();
            col.isTrigger = true;

            // Ground (3) + Player (8)
            destroyOnContact = (1 << 3) | (1 << 8);
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

            RandomizeSprite();
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

        private void FixedUpdate()
        {
            var pos = rb.position;

            if (IsDeflected)
            {
                pos += Vector2.up * (deflectSpeed * Time.fixedDeltaTime);
            }
            else
            {
                elapsed += Time.fixedDeltaTime;
                var prevSway = swayAmplitude * Mathf.Sin((elapsed - Time.fixedDeltaTime) * swayFrequency * Mathf.PI * 2f);
                var currSway = swayAmplitude * Mathf.Sin(elapsed * swayFrequency * Mathf.PI * 2f);
                pos += new Vector2(currSway - prevSway, -fallSpeed * Time.fixedDeltaTime);
            }

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
                combatState.TakeDamage(damage);

            ReturnToPool();
        }

        private void RandomizeSprite()
        {
            if (sprites != null && sprites.Length > 0 && spriteRenderer != null)
                spriteRenderer.sprite = sprites[Random.Range(0, sprites.Length)];
        }
    }
}
