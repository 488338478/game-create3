using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GameCreate3.UI
{
    public enum HUDMode
    {
        Gameplay,
        Dream,
        Story,
        Hidden
    }

    public sealed class UIInGameHUDPageController : UIPageController
    {
        [SerializeField] private TMP_Text taskText;
        [SerializeField] private TMP_Text phaseText;
        [SerializeField] private TMP_Text interactionHintText;
        [SerializeField] private TMP_Text temporaryMessageText;
        [SerializeField] private Button pauseButton;

        private Coroutine temporaryMessageRoutine;

        private void OnEnable()
        {
            if (pauseButton != null)
            {
                pauseButton.onClick.AddListener(HandlePause);
            }
        }

        private void OnDisable()
        {
            if (pauseButton != null)
            {
                pauseButton.onClick.RemoveListener(HandlePause);
            }
        }

        public void SetMode(HUDMode mode)
        {
            var visible = mode != HUDMode.Hidden;
            SetInteractable(visible);
            gameObject.SetActive(visible);

            if (pauseButton != null)
            {
                pauseButton.gameObject.SetActive(mode == HUDMode.Gameplay || mode == HUDMode.Dream);
            }

            if (taskText != null)
            {
                taskText.gameObject.SetActive(mode == HUDMode.Gameplay || mode == HUDMode.Dream);
            }

            if (phaseText != null)
            {
                phaseText.gameObject.SetActive(mode != HUDMode.Hidden);
            }
        }

        public void SetTaskText(string text)
        {
            SetText(taskText, text);
        }

        public void SetPhaseText(string text)
        {
            SetText(phaseText, text);
        }

        public void ShowInteractionHint(string text)
        {
            SetText(interactionHintText, text);
            if (interactionHintText != null)
            {
                interactionHintText.gameObject.SetActive(true);
            }
        }

        public void HideInteractionHint()
        {
            if (interactionHintText != null)
            {
                interactionHintText.gameObject.SetActive(false);
            }
        }

        public void ShowTemporaryMessage(string text, float duration)
        {
            if (temporaryMessageRoutine != null)
            {
                StopCoroutine(temporaryMessageRoutine);
            }

            temporaryMessageRoutine = StartCoroutine(TemporaryMessageRoutine(text, duration));
        }

        private IEnumerator TemporaryMessageRoutine(string text, float duration)
        {
            SetText(temporaryMessageText, text);
            if (temporaryMessageText != null)
            {
                temporaryMessageText.gameObject.SetActive(true);
            }

            yield return new WaitForSecondsRealtime(Mathf.Max(0f, duration));

            if (temporaryMessageText != null)
            {
                temporaryMessageText.gameObject.SetActive(false);
            }

            temporaryMessageRoutine = null;
        }

        private static void SetText(TMP_Text label, string text)
        {
            if (label != null)
            {
                label.text = text ?? string.Empty;
            }
        }

        private static void HandlePause()
        {
            UIControlSystem.Instance?.OpenPage(UIPageIds.PauseMenu);
        }
    }
}
