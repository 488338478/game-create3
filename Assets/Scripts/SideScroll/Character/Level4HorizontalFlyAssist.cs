using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace GameCreate3
{
    [DefaultExecutionOrder(1000)]
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(Animator))]
    public sealed class Level4HorizontalFlyAssist : MonoBehaviour
    {
        [SerializeField] private string targetSceneName = "Level4";
        [SerializeField] private string goalBubbleName = "GoalBubble";
        [SerializeField] private Rigidbody2D targetBody;
        [SerializeField] private Animator targetAnimator;
        [SerializeField] private AnimationClip flyClip;
        [SerializeField] private float hoverYOffset;
        [SerializeField] private float fallbackHoverY = 1.79f;
        [SerializeField] private bool snapToHoverYOnEnable = true;

        [Header("Flappy Flight")]
        [SerializeField] private float flapImpulse = 4f;
        [SerializeField] private float gravityStrength = 6f;
        [SerializeField] private float autoMoveSpeed = 2f;
        [SerializeField] private float maxFallSpeed = -5f;
        [SerializeField] private float maxRiseSpeed = 8f;

        private CharacterInputProxy inputProxy;
        private bool flapThisFrame;
        private float flightYVelocity;
        private RuntimeAnimatorController originalController;
        private AnimatorOverrideController flightController;
        private float originalGravityScale;
        private bool originalStateCached;
        private bool isFlightModeActive;
        private float hoverY;

        public bool IsFlightModeActive => isFlightModeActive;

        private void Awake()
        {
            targetBody = targetBody != null ? targetBody : GetComponent<Rigidbody2D>();
            targetAnimator = targetAnimator != null ? targetAnimator : GetComponent<Animator>();
            inputProxy = inputProxy != null ? inputProxy : GetComponent<CharacterInputProxy>();
            CacheOriginalState();
        }

        private void OnEnable()
        {
            RefreshFlightMode();
        }

        private void Update()
        {
            if (isFlightModeActive && inputProxy != null && inputProxy.JumpPressed)
            {
                flapThisFrame = true;
            }
        }

        private void Start()
        {
            RefreshFlightMode();
        }

        private void FixedUpdate()
        {
            RefreshFlightMode();
            if (!isFlightModeActive || targetBody == null)
            {
                return;
            }

            targetBody.gravityScale = 0f;

            flightYVelocity -= gravityStrength * Time.fixedDeltaTime;

            if (flapThisFrame)
            {
                flightYVelocity = flapImpulse;
                flapThisFrame = false;
            }

            flightYVelocity = Mathf.Clamp(flightYVelocity, maxFallSpeed, maxRiseSpeed);
            targetBody.velocity = new Vector2(autoMoveSpeed, flightYVelocity);
        }

        private void OnDisable()
        {
            RestoreOriginalState();
        }

        private void CacheOriginalState()
        {
            if (originalStateCached || targetBody == null)
            {
                return;
            }

            originalGravityScale = targetBody.gravityScale;
            if (targetAnimator != null)
            {
                originalController = targetAnimator.runtimeAnimatorController;
            }

            originalStateCached = true;
        }

        private void RefreshFlightMode()
        {
            var shouldEnableFlight = SceneManager.GetActiveScene().name == targetSceneName;
            if (shouldEnableFlight)
            {
                if (!isFlightModeActive)
                {
                    EnableFlightMode();
                }
            }
            else if (isFlightModeActive)
            {
                RestoreOriginalState();
            }
        }

        private void EnableFlightMode()
        {
            if (targetBody == null)
            {
                return;
            }

            CacheOriginalState();
            hoverY = ResolveHoverY();
            isFlightModeActive = true;
            targetBody.gravityScale = 0f;

            if (snapToHoverYOnEnable)
            {
                var position = targetBody.position;
                position.y = hoverY;
                targetBody.position = position;
            }

            flightYVelocity = 0f;
            targetBody.velocity = new Vector2(targetBody.velocity.x, 0f);

            ApplyFlightAnimationOverride();
        }

        private float ResolveHoverY()
        {
            var hoverHeight = fallbackHoverY;
            var goalBubble = GameObject.Find(goalBubbleName);
            if (goalBubble != null)
            {
                hoverHeight = goalBubble.transform.position.y + hoverYOffset;
            }

            return hoverHeight;
        }

        private void ApplyFlightAnimationOverride()
        {
            if (targetAnimator == null || flyClip == null || originalController == null)
            {
                return;
            }

            if (flightController == null)
            {
                flightController = new AnimatorOverrideController(originalController);
            }

            var seenClips = new HashSet<AnimationClip>();
            foreach (var clip in originalController.animationClips)
            {
                if (clip == null || !seenClips.Add(clip))
                {
                    continue;
                }

                if (clip.name == "bear_walk" || clip.name == "bear_run")
                {
                    flightController[clip.name] = flyClip;
                }
            }

            if (targetAnimator.runtimeAnimatorController != flightController)
            {
                targetAnimator.runtimeAnimatorController = flightController;
            }
        }

        private void RestoreOriginalState()
        {
            if (!originalStateCached)
            {
                return;
            }

            if (targetBody != null)
            {
                targetBody.gravityScale = originalGravityScale;
            }

            if (targetAnimator != null && originalController != null && targetAnimator.runtimeAnimatorController != originalController)
            {
                targetAnimator.runtimeAnimatorController = originalController;
            }

            isFlightModeActive = false;
        }
    }
}
