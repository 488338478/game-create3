using System;
using UnityEngine;

namespace GameCreate3
{
    public sealed class DreamColorPickup : MonoBehaviour, ISideScrollInteractable
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

        private float moveAngle;
        private float moveSpeed;
        private float minY;
        private bool active;
        private bool grounded;
        private bool manualCollect;
        private float groundedTimer;
        private float lingerDuration;
        private float fadeDuration;
        private float fadeElapsed;
        private Sprite visualSprite;
        private Sprite baseSprite;
        private Vector3 previousPosition;
        private InteractPromptIndicator promptIndicator;

        public PaletteColorOption Option => option;
        public Color Color => option.fallbackColor;
        public bool IsResonant => resonant;
        public bool IsCollected { get; private set; }
        public bool IsActive => active;
        public bool IsGrounded => grounded;
        public Vector3 PreviousPosition => previousPosition;
        public Collider2D PickupCollider => pickupCollider;

        public string Prompt => "拾取";
        public event Action<DreamColorPickup> InteractCollected;

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

            promptIndicator = GetComponent<InteractPromptIndicator>();
            if (promptIndicator == null)
            {
                promptIndicator = gameObject.AddComponent<InteractPromptIndicator>();
            }
            promptIndicator.enabled = false;

            UpdateVisual();
            ReturnToPool();
        }

        public void Activate(PaletteColorOption pickupOption, bool isResonant, Sprite sprite, Vector3 spawnPosition, float angle, float speed, float minYPos)
        {
            option = pickupOption;
            resonant = isResonant;
            visualSprite = sprite;
            moveAngle = angle;
            moveSpeed = speed;
            minY = minYPos;
            active = true;
            IsCollected = false;
            grounded = false;
            groundedTimer = 0f;
            fadeElapsed = 0f;

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

            if (grounded)
            {
                return TickGrounded(deltaTime);
            }

            previousPosition = transform.position;
            var rad = moveAngle * Mathf.Deg2Rad;
            var position = previousPosition;
            position.x += Mathf.Cos(rad) * moveSpeed * deltaTime;
            position.y += Mathf.Sin(rad) * moveSpeed * deltaTime;
            transform.position = position;

            if (visualRoot != null && spinSpeed != 0f)
            {
                visualRoot.Rotate(0f, 0f, spinSpeed * deltaTime);
            }

            if (position.y <= minY)
            {
                if (manualCollect)
                {
                    Ground(lingerDuration > 0f ? lingerDuration : 2f, fadeDuration > 0f ? fadeDuration : 0.3f);
                }
                else
                {
                    Miss();
                }
                return !manualCollect;
            }

            return false;
        }

        private bool TickGrounded(float deltaTime)
        {
            groundedTimer -= deltaTime;
            if (groundedTimer <= 0f)
            {
                fadeElapsed += deltaTime;
                var t = Mathf.Clamp01(fadeElapsed / fadeDuration);
                if (spriteRenderer != null)
                {
                    var c = spriteRenderer.color;
                    c.a = 1f - t;
                    spriteRenderer.color = c;
                }

                if (t >= 1f)
                {
                    Miss();
                    return true;
                }
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
            grounded = false;
            groundedTimer = 0f;
            fadeElapsed = 0f;

            if (spriteRenderer != null)
            {
                var c = spriteRenderer.color;
                c.a = 1f;
                spriteRenderer.color = c;
            }

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

        public bool CanInteract(GameObject interactor)
        {
            return manualCollect && active && !IsCollected;
        }

        public void Interact(GameObject interactor)
        {
            if (!manualCollect || !active || IsCollected) return;
            InteractCollected?.Invoke(this);
        }

        public void SetManualCollect(bool enabled)
        {
            manualCollect = enabled;
            if (promptIndicator != null)
            {
                promptIndicator.enabled = enabled;
            }
        }

        public void SetLingerParams(float linger, float fade)
        {
            lingerDuration = linger;
            fadeDuration = Mathf.Max(0.05f, fade);
        }

        public void Ground(float linger, float fade)
        {
            if (!active || IsCollected || grounded) return;

            if (!manualCollect)
            {
                Miss();
                return;
            }

            grounded = true;
            moveSpeed = 0f;
            groundedTimer = linger;
            lingerDuration = linger;
            fadeDuration = Mathf.Max(0.05f, fade);
            fadeElapsed = 0f;

            if (pickupCollider != null)
            {
                pickupCollider.isTrigger = true;
                pickupCollider.enabled = true;
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
