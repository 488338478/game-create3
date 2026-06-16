using UnityEngine;

namespace GameCreate3
{
    public sealed class DreamColorPickup : MonoBehaviour
    {
        [SerializeField] private PaletteColorOption option;
        [SerializeField] private bool resonant = true;
        [SerializeField] private SpriteRenderer spriteRenderer;
        [SerializeField] private Collider2D pickupCollider;
        [SerializeField] private Transform visualRoot;
        [SerializeField] private GameObject visualObject;
        [SerializeField] private float spinSpeed = 0f;
        [SerializeField] private float resonantScale = 0.72f;
        [SerializeField] private float mutedScale = 0.64f;

        private float verticalSpeed;
        private float fallAcceleration;
        private float horizontalDrift;
        private float groundY;
        private bool active;
        private Sprite visualSprite;
        private Sprite baseSprite;
        private Vector3 previousPosition;

        public PaletteColorOption Option => option;
        public Color Color => option.fallbackColor;
        public bool IsResonant => resonant;
        public bool IsCollected { get; private set; }
        public bool IsActive => active;
        public Vector3 PreviousPosition => previousPosition;
        public Collider2D PickupCollider => pickupCollider;

        private void Awake()
        {
            if (spriteRenderer == null)
            {
                spriteRenderer = GetComponentInChildren<SpriteRenderer>(true);
            }

            if (pickupCollider == null)
            {
                pickupCollider = GetComponentInChildren<Collider2D>(true);
            }

            if (visualRoot == null)
            {
                visualRoot = spriteRenderer != null ? spriteRenderer.transform : transform;
            }

            if (visualObject == null)
            {
                visualObject = spriteRenderer != null ? spriteRenderer.gameObject : gameObject;
            }

            baseSprite = spriteRenderer != null ? spriteRenderer.sprite : null;
            UpdateVisual();
            ReturnToPool();
        }

        public void Activate(PaletteColorOption pickupOption, bool isResonant, Sprite sprite, Vector3 spawnPosition, float initialFallSpeed, float acceleration, float driftX, float floorY)
        {
            option = pickupOption;
            resonant = isResonant;
            visualSprite = sprite;
            verticalSpeed = initialFallSpeed;
            fallAcceleration = acceleration;
            horizontalDrift = driftX;
            groundY = floorY;
            active = true;
            IsCollected = false;

            transform.position = spawnPosition;
            previousPosition = spawnPosition;
            transform.rotation = Quaternion.identity;
            transform.localScale = Vector3.one;
            gameObject.SetActive(true);

            if (visualObject != null)
            {
                visualObject.SetActive(true);
            }

            if (visualRoot != null)
            {
                visualRoot.localRotation = Quaternion.identity;
            }

            if (pickupCollider != null)
            {
                pickupCollider.enabled = true;
            }

            UpdateVisual();
        }

        public bool Tick(float deltaTime)
        {
            if (!active || IsCollected)
            {
                return false;
            }

            verticalSpeed += fallAcceleration * deltaTime;

            previousPosition = transform.position;
            var position = previousPosition;
            position.x += horizontalDrift * deltaTime;
            position.y -= verticalSpeed * deltaTime;
            transform.position = position;

            if (visualRoot != null && spinSpeed != 0f)
            {
                visualRoot.Rotate(0f, 0f, spinSpeed * deltaTime);
            }

            if (position.y <= groundY)
            {
                Miss();
                return true;
            }

            return false;
        }

        public void Collect()
        {
            if (!active || IsCollected)
            {
                return;
            }

            IsCollected = true;
            active = false;

            if (visualObject != null)
            {
                visualObject.SetActive(false);
            }

            if (pickupCollider != null)
            {
                pickupCollider.enabled = false;
            }

            gameObject.SetActive(false);
        }

        public void Miss()
        {
            if (!active)
            {
                return;
            }

            active = false;
            IsCollected = false;

            if (visualObject != null)
            {
                visualObject.SetActive(false);
            }

            if (pickupCollider != null)
            {
                pickupCollider.enabled = false;
            }

            gameObject.SetActive(false);
        }

        public void ReturnToPool()
        {
            active = false;
            IsCollected = false;

            if (visualObject != null)
            {
                visualObject.SetActive(false);
            }

            if (pickupCollider != null)
            {
                pickupCollider.enabled = false;
            }

            gameObject.SetActive(false);
        }

        public void SetInteractive(bool enabled)
        {
            if (pickupCollider != null)
            {
                pickupCollider.enabled = enabled && active && !IsCollected;
            }
        }

        private void UpdateVisual()
        {
            if (spriteRenderer != null)
            {
                spriteRenderer.sprite = visualSprite != null
                    ? visualSprite
                    : (option.paletteSprite != null ? option.paletteSprite : baseSprite);

                if (spriteRenderer.sprite != null)
                {
                    spriteRenderer.color = Color.white;
                }
                else
                {
                    spriteRenderer.color = resonant
                        ? option.fallbackColor
                        : Color.Lerp(option.fallbackColor, Color.black, 0.28f);
                }
            }

            var targetScale = resonant ? resonantScale : mutedScale;
            if (visualRoot != null)
            {
                visualRoot.localScale = Vector3.one * targetScale;
            }
        }
    }
}
