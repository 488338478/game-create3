using System.Collections;
using GameCreate3.Core;
using UnityEngine;
using UnityEngine.UI;

namespace GameCreate3.UI
{
    [DisallowMultipleComponent]
    public sealed class UINotifyBlinkOnStart : MonoBehaviour
    {
        [SerializeField] private Graphic targetGraphic;
        [SerializeField] private AudioClip notificationClip;
        [SerializeField] [Min(0f)] private float startDelay = 0.2f;
        [SerializeField] [Min(0.01f)] private float blinkStepSeconds = 0.08f;
        [SerializeField] [Range(0.05f, 1f)] private float minimumAlpha = 0.15f;
        [SerializeField] [Min(1)] private int blinkCount = 2;
        [SerializeField] [Range(0f, 1f)] private float volumeScale = 1f;

        private CanvasGroup canvasGroup;
        private Coroutine playRoutine;
        private float initialAlpha = 1f;
        private bool hasPlayed;

        private void Awake()
        {
            if (targetGraphic == null)
            {
                targetGraphic = GetComponent<Graphic>();
            }

            canvasGroup = GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                canvasGroup = gameObject.AddComponent<CanvasGroup>();
            }

            initialAlpha = Mathf.Clamp01(canvasGroup.alpha);
        }

        private void Start()
        {
            if (hasPlayed)
            {
                return;
            }

            hasPlayed = true;
            playRoutine = StartCoroutine(PlayRoutine());
        }

        private void OnDisable()
        {
            if (playRoutine != null)
            {
                StopCoroutine(playRoutine);
                playRoutine = null;
            }

            SetAlpha(initialAlpha);
        }

        private IEnumerator PlayRoutine()
        {
            if (startDelay > 0f)
            {
                yield return new WaitForSecondsRealtime(startDelay);
            }

            PlayNotification();

            for (var i = 0; i < blinkCount; i++)
            {
                SetAlpha(minimumAlpha);
                yield return new WaitForSecondsRealtime(blinkStepSeconds);

                SetAlpha(initialAlpha);
                if (i < blinkCount - 1)
                {
                    yield return new WaitForSecondsRealtime(blinkStepSeconds);
                }
            }

            playRoutine = null;
        }

        private void PlayNotification()
        {
            if (notificationClip == null)
            {
                return;
            }

            if (GameAudioService.Instance != null)
            {
                GameAudioService.Instance.PlaySFX(notificationClip, GameAudioChannel.Ui, volumeScale);
                return;
            }

            var audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
                audioSource.playOnAwake = false;
                audioSource.spatialBlend = 0f;
            }

            audioSource.PlayOneShot(notificationClip, Mathf.Clamp01(volumeScale));
        }

        private void SetAlpha(float alpha)
        {
            if (canvasGroup != null)
            {
                canvasGroup.alpha = Mathf.Clamp01(alpha);
                return;
            }

            if (targetGraphic == null)
            {
                return;
            }

            var color = targetGraphic.color;
            color.a = Mathf.Clamp01(alpha);
            targetGraphic.color = color;
        }
    }
}
