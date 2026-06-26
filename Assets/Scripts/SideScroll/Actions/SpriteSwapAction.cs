using System.Collections;
using UnityEngine;

namespace GameCreate3
{
    [RequireComponent(typeof(SpriteRenderer))]
    public sealed class SpriteSwapAction : MonoBehaviour
    {
        [SerializeField] private Sprite alternateSprite;
        [SerializeField] private Animator syncAnimator;
        [SerializeField] private SpriteRenderer syncedEffectRenderer;
        [SerializeField] private float temporaryDuration = 0.75f;
        [SerializeField] private bool preferAnimatorLifetime = true;
        [SerializeField] private bool restartAnimatorOnTemporarySwap = true;

        private SpriteRenderer sr;
        private Sprite originalSprite;
        private bool swapped;
        private Coroutine temporarySwapRoutine;

        private void Awake()
        {
            sr = GetComponent<SpriteRenderer>();
            originalSprite = sr.sprite;
            if (syncAnimator == null)
                syncAnimator = GetComponent<Animator>();
            if (syncedEffectRenderer == null)
            {
                var childRenderers = GetComponentsInChildren<SpriteRenderer>(true);
                foreach (var childRenderer in childRenderers)
                {
                    if (childRenderer != sr)
                    {
                        syncedEffectRenderer = childRenderer;
                        break;
                    }
                }
            }
        }

        private void OnDisable()
        {
            StopTemporarySwapRoutine();
            if (sr != null)
                sr.sprite = originalSprite;
            ClearEffectVisual();
            swapped = false;
        }

        public void Swap()
        {
            if (alternateSprite == null) return;
            StopTemporarySwapRoutine();
            swapped = !swapped;
            sr.sprite = swapped ? alternateSprite : originalSprite;
        }

        public void SetAlternate()
        {
            if (alternateSprite == null) return;
            StopTemporarySwapRoutine();
            swapped = true;
            sr.sprite = alternateSprite;
        }

        public void SetOriginal()
        {
            StopTemporarySwapRoutine();
            swapped = false;
            sr.sprite = originalSprite;
            ClearEffectVisual();
        }

        public void SetAlternateTemporarily()
        {
            if (alternateSprite == null) return;

            SetAlternate();
            ClearEffectVisual();

            if (!isActiveAndEnabled)
                return;

            if (preferAnimatorLifetime && syncAnimator != null && syncAnimator.runtimeAnimatorController != null)
            {
                if (restartAnimatorOnTemporarySwap)
                    RestartAnimator();
                temporarySwapRoutine = StartCoroutine(RestoreWhenAnimatorStops());
                return;
            }

            temporarySwapRoutine = StartCoroutine(RestoreAfterDelay());
        }

        private IEnumerator RestoreAfterDelay()
        {
            var duration = temporaryDuration > 0f ? temporaryDuration : 0.75f;
            yield return new WaitForSeconds(duration);
            temporarySwapRoutine = null;
            SetOriginal();
        }

        private IEnumerator RestoreWhenAnimatorStops()
        {
            const float StartupTimeout = 0.2f;
            var startupElapsed = 0f;

            while (syncAnimator != null &&
                   syncAnimator.isActiveAndEnabled &&
                   !HasVisibleMotion(syncAnimator) &&
                   startupElapsed < StartupTimeout)
            {
                startupElapsed += Time.deltaTime;
                yield return null;
            }

            if (syncAnimator == null || !syncAnimator.isActiveAndEnabled)
            {
                temporarySwapRoutine = null;
                SetOriginal();
                yield break;
            }

            if (!HasVisibleMotion(syncAnimator))
            {
                yield return RestoreAfterDelay();
                yield break;
            }

            while (syncAnimator != null &&
                   syncAnimator.isActiveAndEnabled &&
                   (HasVisibleMotion(syncAnimator) || syncAnimator.IsInTransition(0)))
            {
                yield return null;
            }

            temporarySwapRoutine = null;
            SetOriginal();
        }

        private void RestartAnimator()
        {
            if (syncAnimator == null) return;
            syncAnimator.enabled = false;
            syncAnimator.enabled = true;
        }

        private static bool HasVisibleMotion(Animator animator)
        {
            return animator.GetCurrentAnimatorClipInfoCount(0) > 0;
        }

        private void StopTemporarySwapRoutine()
        {
            if (temporarySwapRoutine == null) return;
            StopCoroutine(temporarySwapRoutine);
            temporarySwapRoutine = null;
        }

        private void ClearEffectVisual()
        {
            if (syncedEffectRenderer != null)
                syncedEffectRenderer.sprite = null;
        }
    }
}
