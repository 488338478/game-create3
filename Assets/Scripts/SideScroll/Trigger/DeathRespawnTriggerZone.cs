using System.Collections;
using Cinemachine;
using UnityEngine;

namespace GameCreate3
{
    public sealed class DeathRespawnTriggerZone : TriggerZoneBase
    {
        [Header("Timing")]
        [SerializeField] private float startDelay;

        [Header("Target")]
        [SerializeField] private Transform respawnTarget;

        [Header("Player animation")]
        [SerializeField] private Animator playerAnimator;
        [SerializeField] private string deathTrigger = "Death";
        [SerializeField] private string respawnTrigger = "Respawn";

        [Header("catCar")]
        [SerializeField] private Transform catCar;
        [SerializeField] private Transform catCarStartPoint;
        [SerializeField] private Transform catCarRenderSlot;
        [SerializeField] private bool speedMode;
        [SerializeField] private float pickupDuration = 0.75f;
        [SerializeField] private float pickupDelay = 0.35f;

        [Header("Return motion")]
        [SerializeField] private float returnDuration = 1.4f;
        [SerializeField] private float arcHeight = 2.2f;
        [SerializeField] private float waveAmplitude = 0.22f;
        [SerializeField] private float waveCyclesPerSecond = 4f;
        [SerializeField] private float waveAmplitudeMin = 0.04f;

        [Header("Camera")]
        [SerializeField] private CinemachineVirtualCamera virtualCamera;
        [SerializeField] private Transform cameraFollowProxy;
        [SerializeField] private float cameraBlendToCatCarDuration = 0.35f;
        [SerializeField] private float cameraBlendToPlayerDuration = 0.35f;

        [Header("Respawn bubble")]
        [SerializeField] private Animator respawnBubble;

        private Coroutine activeRoutine;
        private Coroutine activeCameraBlend;
        private bool catCarWasInactive;
        private Collider2D cameraBounds;
        private CinemachineVirtualCamera activeCamera;
        private Transform originalCameraFollow;
        private Transform cameraBlendTarget;
        private bool cameraBlendInProgress;

        protected override bool CanTrigger(Collider2D other)
        {
            return activeRoutine == null &&
                base.CanTrigger(other) &&
                other.TryGetComponent<SideScrollCharacterControllerBase>(out _);
        }

        protected override void OnTriggered(Collider2D other)
        {
            if (!other.TryGetComponent<SideScrollCharacterControllerBase>(out var player))
            {
                return;
            }

            GameCreate3.Core.GameAudioService.Instance?.PlaySFX("SFX_Fall_Down");
            activeRoutine = StartCoroutine(RespawnRoutine(player));
        }

        private IEnumerator RespawnRoutine(SideScrollCharacterControllerBase player)
        {
            var playerTransform = player.transform;
            var body = player.GetComponent<Rigidbody2D>();
            var animatorDriver = player.GetComponentInChildren<SideScrollCharacterAnimatorDriver>(true);
            var colliders = player.GetComponentsInChildren<Collider2D>(true);
            var colliderStates = new bool[colliders.Length];

            var hadBody = body != null;
            var previousSimulated = hadBody && body.simulated;
            var previousInputEnabled = player.InputEnabled;
            var cameraToBlend = ResolveVirtualCamera();
            cameraBounds = Workspace != null && Workspace.CameraController != null ? Workspace.CameraController.BoundingShape : null;
            activeCamera = cameraToBlend;
            originalCameraFollow = activeCamera != null ? activeCamera.Follow : null;
            var catCarHomePosition = catCarStartPoint != null
                ? catCarStartPoint.position
                : catCar != null
                    ? catCar.position
                    : Vector3.zero;

            catCarWasInactive = catCar != null && !catCar.gameObject.activeSelf;
            if (catCarWasInactive)
                catCar.gameObject.SetActive(true);

            for (var i = 0; i < colliders.Length; i++)
            {
                colliderStates[i] = colliders[i].enabled;
                colliders[i].enabled = false;
            }

            player.SetInputEnabled(false);
            if (hadBody)
            {
                body.velocity = Vector2.zero;
                body.angularVelocity = 0f;
                body.simulated = false;
            }

            // 接管开始：锁死动画驱动，泡泡搬运期间不再被 Speed/Jump 乱切。
            if (animatorDriver != null)
                animatorDriver.SetAnimationFrozen(true);

            TriggerAnimation(player, playerAnimator, deathTrigger);

            if (startDelay > 0f)
                yield return new WaitForSeconds(startDelay);

            if (respawnBubble != null)
            {
                respawnBubble.gameObject.SetActive(true);
                respawnBubble.Play("bubble_split", 0, 0f);
            }

            StartCameraBlend(cameraToBlend, catCar, cameraBlendToCatCarDuration);

            var pickupPoint = playerTransform.position;
            yield return MoveCatCarToPickupPoint(pickupPoint);

            if (pickupDelay > 0f)
            {
                StartCameraBlend(cameraToBlend, playerTransform, cameraBlendToPlayerDuration);
                yield return HoverAtPickupPoint(playerTransform, pickupDelay);
            }
            else
            {
                StartCameraBlend(cameraToBlend, playerTransform, cameraBlendToPlayerDuration);
            }

            var target = ResolveRespawnTarget(playerTransform.position);
            var start = HasCarrierSlot ? catCarRenderSlot.position : playerTransform.position;

            yield return MovePlayerToTarget(playerTransform, start, target);

            playerTransform.position = target;
            if (catCar != null && !HasCarrierSlot)
            {
                catCar.position = target;
            }

            if (hadBody)
            {
                body.simulated = previousSimulated;
                body.velocity = Vector2.zero;
                body.angularVelocity = 0f;
            }

            var flyAssist = player.GetComponent<Level4HorizontalFlyAssist>();
            if (flyAssist != null && flyAssist.IsFlightModeActive)
            {
                flyAssist.ResetFlightState();
            }

            for (var i = 0; i < colliders.Length; i++)
            {
                colliders[i].enabled = colliderStates[i];
            }

            player.SetInputEnabled(true);
            // 接管结束：解冻后再播放重生动画，之后交还给正常驱动。
            if (animatorDriver != null)
                animatorDriver.SetAnimationFrozen(false);
            TriggerAnimation(player, playerAnimator, respawnTrigger);
            RestoreCameraFollow(playerTransform);
            RestoreCatCar(catCarHomePosition);

            if (respawnBubble != null)
            {
                respawnBubble.gameObject.SetActive(false);
            }

            activeRoutine = null;
        }

        private void RestoreCatCar(Vector3 homePosition)
        {
            if (catCar == null) return;

            catCar.position = homePosition;

            if (catCarWasInactive)
                catCar.gameObject.SetActive(false);
        }

        private CinemachineVirtualCamera ResolveVirtualCamera()
        {
            if (virtualCamera != null)
            {
                return virtualCamera;
            }

            if (Workspace != null && Workspace.CameraController != null)
            {
                virtualCamera = Workspace.CameraController.VirtualCamera;
            }

            return virtualCamera;
        }

        private void StartCameraBlend(CinemachineVirtualCamera cameraToBlend, Transform target, float duration)
        {
            if (cameraToBlend == null || target == null)
            {
                return;
            }

            if (activeCameraBlend != null)
            {
                StopCoroutine(activeCameraBlend);
                cameraBlendInProgress = false;
            }

            cameraBlendTarget = target;
            activeCameraBlend = StartCoroutine(BlendCameraFollow(cameraToBlend, target, duration));
        }

        private IEnumerator BlendCameraFollow(CinemachineVirtualCamera cameraToBlend, Transform target, float duration)
        {
            cameraBlendInProgress = true;
            var proxy = ResolveCameraFollowProxy();
            var start = ResolveCameraFollowPosition(cameraToBlend);
            proxy.position = start;
            cameraToBlend.Follow = proxy;

            var elapsed = 0f;
            var safeDuration = Mathf.Max(0.01f, duration);
            while (elapsed < safeDuration)
            {
                var t = Smooth01(Mathf.Clamp01(elapsed / safeDuration));
                proxy.position = ClampCameraFollowPosition(Vector3.Lerp(start, target.position, t));
                elapsed += Time.deltaTime;
                yield return null;
            }

            proxy.position = ClampCameraFollowPosition(target.position);
            cameraBlendInProgress = false;
            activeCameraBlend = null;
        }

        private void LateUpdate()
        {
            if (cameraBlendInProgress ||
                activeRoutine == null ||
                activeCamera == null ||
                cameraBlendTarget == null ||
                cameraFollowProxy == null)
            {
                return;
            }

            if (activeCamera.Follow == cameraFollowProxy)
            {
                cameraFollowProxy.position = ClampCameraFollowPosition(cameraBlendTarget.position);
            }
        }

        private void RestoreCameraFollow(Transform fallback)
        {
            if (activeCameraBlend != null)
            {
                StopCoroutine(activeCameraBlend);
                activeCameraBlend = null;
            }

            cameraBlendInProgress = false;
            if (activeCamera != null)
            {
                activeCamera.Follow = originalCameraFollow != null ? originalCameraFollow : fallback;
            }

            activeCamera = null;
            originalCameraFollow = null;
            cameraBlendTarget = null;
            cameraBounds = null;
        }

        private Transform ResolveCameraFollowProxy()
        {
            if (cameraFollowProxy != null)
            {
                return cameraFollowProxy;
            }

            var proxy = new GameObject($"{name}_CameraFollowProxy");
            proxy.hideFlags = HideFlags.HideInHierarchy;
            cameraFollowProxy = proxy.transform;
            return cameraFollowProxy;
        }

        private static Vector3 ResolveCameraFollowPosition(CinemachineVirtualCamera cameraToBlend)
        {
            if (cameraToBlend.Follow != null)
            {
                return cameraToBlend.Follow.position;
            }

            return cameraToBlend.transform.position;
        }

        private Vector3 ClampCameraFollowPosition(Vector3 position)
        {
            if (cameraBounds == null)
            {
                return position;
            }

            var bounds = cameraBounds.bounds;
            position.x = Mathf.Clamp(position.x, bounds.min.x, bounds.max.x);
            position.y = Mathf.Clamp(position.y, bounds.min.y, bounds.max.y);
            return position;
        }

        private IEnumerator MoveCatCarToPickupPoint(Vector3 pickupPoint)
        {
            if (catCar == null)
            {
                yield break;
            }

            var start = catCarStartPoint != null ? catCarStartPoint.position : catCar.position;
            yield return MoveTransformWithWave(catCar, start, pickupPoint, ResolveDuration(start, pickupPoint, pickupDuration));
        }

        private IEnumerator HoverAtPickupPoint(Transform playerTransform, float duration)
        {
            var elapsed = 0f;
            var basePosition = catCar != null ? catCar.position : playerTransform.position;
            var safeDuration = Mathf.Max(0.01f, duration);

            while (elapsed < safeDuration)
            {
                var offsetY = Mathf.Sin(elapsed * Mathf.PI * 2f * waveCyclesPerSecond) * waveAmplitudeMin;
                if (catCar != null)
                {
                    catCar.position = basePosition + Vector3.up * offsetY;
                }

                playerTransform.position = HasCarrierSlot ? catCarRenderSlot.position : basePosition + Vector3.up * offsetY;
                elapsed += Time.deltaTime;
                yield return null;
            }

            if (catCar != null)
            {
                catCar.position = basePosition;
            }

            playerTransform.position = HasCarrierSlot ? catCarRenderSlot.position : basePosition;
        }

        private IEnumerator MovePlayerToTarget(Transform playerTransform, Vector3 start, Vector3 target)
        {
            var elapsed = 0f;
            var duration = ResolveDuration(start, target, returnDuration);
            var carrierSlotOffset = HasCarrierSlot ? catCarRenderSlot.position - catCar.position : Vector3.zero;

            while (elapsed < duration)
            {
                var t = Mathf.Clamp01(elapsed / duration);
                var position = EvaluateArcWave(start, target, t, elapsed);
                if (catCar != null)
                {
                    catCar.position = HasCarrierSlot ? position - carrierSlotOffset : position;
                }

                playerTransform.position = HasCarrierSlot ? catCarRenderSlot.position : position;
                elapsed += Time.deltaTime;
                yield return null;
            }
        }

        private IEnumerator MoveTransformWithWave(Transform targetTransform, Vector3 start, Vector3 end, float duration)
        {
            var elapsed = 0f;
            var safeDuration = Mathf.Max(0.01f, duration);

            while (elapsed < safeDuration)
            {
                var t = Mathf.Clamp01(elapsed / safeDuration);
                targetTransform.position = EvaluateWave(start, end, t, elapsed);
                elapsed += Time.deltaTime;
                yield return null;
            }

            targetTransform.position = end;
        }

        private Vector3 ResolveRespawnTarget(Vector3 fallback)
        {
            if (respawnTarget != null)
            {
                return respawnTarget.position;
            }

            if (Workspace != null && Workspace.PlayerSpawn != null)
            {
                return Workspace.PlayerSpawn.position;
            }

            return fallback;
        }

        private bool HasCarrierSlot => catCar != null && catCarRenderSlot != null;

        private float ResolveDuration(Vector3 start, Vector3 end, float durationOrSpeed)
        {
            var safeValue = Mathf.Max(0.01f, durationOrSpeed);
            if (!speedMode)
            {
                return safeValue;
            }

            return Mathf.Max(0.01f, Vector3.Distance(start, end) / safeValue);
        }

        private Vector3 EvaluateArcWave(Vector3 start, Vector3 end, float t, float elapsed)
        {
            var basePosition = Vector3.Lerp(start, end, Smooth01(t));
            var arcFactor = 4f * t * (1f - t);
            var arc = arcHeight * arcFactor;
            var wave = Mathf.Sin(elapsed * Mathf.PI * 2f * waveCyclesPerSecond) * waveAmplitudeMin * arcFactor;
            basePosition.y += arc + wave;
            return basePosition;
        }

        private Vector3 EvaluateWave(Vector3 start, Vector3 end, float t, float elapsed)
        {
            var basePosition = Vector3.Lerp(start, end, Smooth01(t));
            var amplitude = Mathf.Lerp(waveAmplitude, waveAmplitudeMin, t);
            basePosition.y += Mathf.Sin(elapsed * Mathf.PI * 2f * waveCyclesPerSecond) * amplitude;
            return basePosition;
        }

        private static float Smooth01(float value)
        {
            return value * value * (3f - 2f * value);
        }

        private static void TriggerAnimation(SideScrollCharacterControllerBase player, Animator explicitAnimator, string triggerName)
        {
            if (string.IsNullOrWhiteSpace(triggerName))
            {
                return;
            }

            var animator = explicitAnimator != null ? explicitAnimator : player.GetComponentInChildren<Animator>(true);
            if (animator != null && HasParameter(animator, triggerName))
            {
                animator.SetTrigger(triggerName);
            }
        }

        private static bool HasParameter(Animator animator, string parameterName)
        {
            var parameters = animator.parameters;
            for (var i = 0; i < parameters.Length; i++)
            {
                if (parameters[i].name == parameterName)
                {
                    return true;
                }
            }

            return false;
        }

    }
}
