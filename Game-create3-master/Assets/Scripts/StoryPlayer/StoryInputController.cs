using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace GameCreate3.StoryPlayer
{
    public sealed class StoryInputController : MonoBehaviour, IPointerClickHandler, IPointerDownHandler, IPointerUpHandler
    {
        [Header("Settings")]
        [SerializeField] private float skipHoldDuration = 1.5f;
        [SerializeField] private float fastForwardThreshold = 0.2f;
        [SerializeField] private bool enableKeyboardInput = true;
        [SerializeField] private KeyCode advanceKey = KeyCode.Space;
        [SerializeField] private KeyCode skipKey = KeyCode.Escape;

        [Header("UI Feedback")]
        [SerializeField] private CanvasGroup skipIndicator;
        [SerializeField] private UnityEngine.UI.Image skipProgressBar;

        private StoryPlayer storyPlayer;
        private StoryPageRenderer pageRenderer;

        private bool isInputEnabled = true;
        private bool isPointerDown;
        private float pointerDownTime;
        private float holdDuration;
        private bool skipTriggered;

        public bool IsInputEnabled
        {
            get => isInputEnabled;
            set => isInputEnabled = value;
        }

        public event Action OnNextPageRequested;
        public event Action OnSkipSequenceRequested;
        public event Action OnTextFastForwardRequested;

        private void Update()
        {
            if (!isInputEnabled)
            {
                return;
            }

            HandleKeyboardInput();
            HandleHoldProgress();
        }

        private void HandleKeyboardInput()
        {
            if (!enableKeyboardInput)
            {
                return;
            }

            if (Input.GetKeyDown(advanceKey))
            {
                HandleAdvanceInput();
            }

            if (Input.GetKeyDown(skipKey))
            {
                HandleSkipInput();
            }
        }

        private void HandleHoldProgress()
        {
            if (!isPointerDown || skipTriggered)
            {
                return;
            }

            holdDuration = Time.time - pointerDownTime;

            UpdateSkipIndicator(holdDuration / skipHoldDuration);

            if (holdDuration >= skipHoldDuration)
            {
                skipTriggered = true;
                HideSkipIndicator();
                HandleSkipInput();
            }
        }

        public void Initialize(StoryPlayer player, StoryPageRenderer renderer)
        {
            storyPlayer = player;
            pageRenderer = renderer;

            if (storyPlayer != null)
            {
                storyPlayer.OnStateChanged += HandleStateChanged;
            }

            HideSkipIndicator();
        }

        private void OnDestroy()
        {
            if (storyPlayer != null)
            {
                storyPlayer.OnStateChanged -= HandleStateChanged;
            }
        }

        private void HandleStateChanged(StoryPlayerState state)
        {
            switch (state)
            {
                case StoryPlayerState.Transitioning:
                    isInputEnabled = false;
                    break;
                case StoryPlayerState.PlayingPage:
                case StoryPlayerState.WaitingInput:
                    isInputEnabled = true;
                    break;
                case StoryPlayerState.Idle:
                case StoryPlayerState.Completed:
                case StoryPlayerState.Skipped:
                    isInputEnabled = false;
                    break;
            }
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (!isInputEnabled)
            {
                return;
            }

            if (skipTriggered)
            {
                return;
            }

            var clickDuration = Time.time - pointerDownTime;

            if (clickDuration >= skipHoldDuration)
            {
                return;
            }

            if (clickDuration <= fastForwardThreshold || holdDuration < skipHoldDuration)
            {
                HandleAdvanceInput();
            }
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (!isInputEnabled)
            {
                return;
            }

            isPointerDown = true;
            pointerDownTime = Time.time;
            holdDuration = 0f;
            skipTriggered = false;
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            isPointerDown = false;
            HideSkipIndicator();
        }

        private void HandleAdvanceInput()
        {
            if (!isInputEnabled)
            {
                return;
            }

            if (pageRenderer != null && pageRenderer.IsRendering)
            {
                OnTextFastForwardRequested?.Invoke();
                return;
            }

            if (storyPlayer != null && storyPlayer.CurrentState == StoryPlayerState.WaitingInput)
            {
                OnNextPageRequested?.Invoke();
                return;
            }

            OnNextPageRequested?.Invoke();
        }

        private void HandleSkipInput()
        {
            if (!isInputEnabled)
            {
                return;
            }

            if (storyPlayer != null && !storyPlayer.CanSkip)
            {
                return;
            }

            OnSkipSequenceRequested?.Invoke();
        }

        private void UpdateSkipIndicator(float progress)
        {
            if (skipIndicator != null)
            {
                skipIndicator.alpha = Mathf.Clamp01(progress * 2f);
            }

            if (skipProgressBar != null)
            {
                skipProgressBar.fillAmount = Mathf.Clamp01(progress);
            }
        }

        private void HideSkipIndicator()
        {
            if (skipIndicator != null)
            {
                skipIndicator.alpha = 0f;
            }

            if (skipProgressBar != null)
            {
                skipProgressBar.fillAmount = 0f;
            }
        }

        public void SetInputEnabled(bool enabled)
        {
            isInputEnabled = enabled;
        }

        public void BlockInputTemporarily(float duration)
        {
            _ = BlockInputAsync(duration);
        }

        private async System.Threading.Tasks.Task BlockInputAsync(float duration)
        {
            isInputEnabled = false;
            await System.Threading.Tasks.Task.Delay(TimeSpan.FromSeconds(duration));
            isInputEnabled = true;
        }
    }
}
