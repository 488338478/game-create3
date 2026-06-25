using System;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace GameCreate3.StoryPlayer
{
    public sealed class StoryPlayer : MonoBehaviour
    {
        [Header("Dependencies")]
        [SerializeField] private StoryVariableStore variableStore;
        [SerializeField] private StoryEventSystem eventSystem;

        private IAudioService audioService;

        [Header("Settings")]
        [SerializeField] private StoryPlaybackMode defaultPlaybackMode = StoryPlaybackMode.ClickToAdvance;
        [SerializeField] private bool allowSkip = true;
        [SerializeField] private float fastForwardSpeed = 5f;

        private IStoryPageRenderer pageRenderer;
        private ITransitionController transitionController;
        private StorySequence currentSequence;
        private StoryPlayerState currentState = StoryPlayerState.Idle;
        private int currentPageIndex = -1;
        private CancellationTokenSource playbackCts;
        private TaskCompletionSource<bool> inputWaitTcs;
        private bool isSkipping = false;
        private float playbackSpeed = 1f;

        public StoryPlayerState CurrentState => currentState;
        public int CurrentPageIndex => currentPageIndex;
        public StorySequence CurrentSequence => currentSequence;
        public bool IsPlaying => currentState != StoryPlayerState.Idle && currentState != StoryPlayerState.Completed;
        public bool CanSkip => allowSkip && currentSequence?.AllowSkip != false;

        public event Action<int> OnPageChanged;
        public event Action<StoryPlayerState> OnStateChanged;
        public event Action OnSequenceCompleted;
        public event Action OnSequenceSkipped;
        public event Action<StoryPageEvent> OnPageEvent;

        private bool initialized;

        public void Initialize(
            IStoryPageRenderer renderer,
            ITransitionController transition,
            IAudioService audio = null,
            StoryEventSystem events = null,
            StoryVariableStore store = null)
        {
            if (initialized) return;
            initialized = true;
            pageRenderer = renderer ?? throw new ArgumentNullException(nameof(renderer));
            transitionController = transition ?? throw new ArgumentNullException(nameof(transition));
            audioService = audio;

            if (events != null) eventSystem = events;
            if (store != null) variableStore = store;

            if (eventSystem != null)
            {
                eventSystem.Initialize(audioService, variableStore);
            }

            pageRenderer.OnRenderComplete += HandleRenderComplete;
            pageRenderer.OnInputRequested += HandleInputRequested;
        }

        private void OnDestroy()
        {
            Cleanup();
        }

        private void Cleanup()
        {
            playbackCts?.Cancel();
            playbackCts?.Dispose();
            playbackCts = null;

            if (pageRenderer != null)
            {
                pageRenderer.OnRenderComplete -= HandleRenderComplete;
                pageRenderer.OnInputRequested -= HandleInputRequested;
            }
        }

        public void Play(StorySequence sequence)
        {
            if (sequence == null)
            {
                Debug.LogError("[StoryPlayer] Cannot play null sequence.");
                return;
            }

            if (currentState != StoryPlayerState.Idle)
            {
                Stop();
            }

            currentSequence = sequence;
            currentPageIndex = -1;
            isSkipping = false;
            playbackSpeed = 1f;

            pageRenderer?.SetSequenceFont(sequence.SequenceFont);
            pageRenderer?.SetPlaybackSpeed(playbackSpeed);
            playbackCts = new CancellationTokenSource();

            SetState(StoryPlayerState.PlayingPage);
            _ = PlaySequenceAsync(playbackCts.Token);
        }

        public void Pause()
        {
            if (currentState != StoryPlayerState.PlayingPage && currentState != StoryPlayerState.WaitingInput)
            {
                return;
            }

            playbackCts?.Cancel();
            SetState(StoryPlayerState.Idle);
        }

        public void Resume()
        {
            if (currentSequence == null || currentState != StoryPlayerState.Idle)
            {
                return;
            }

            playbackCts = new CancellationTokenSource();

            if (currentPageIndex < 0)
            {
                SetState(StoryPlayerState.PlayingPage);
                _ = PlaySequenceAsync(playbackCts.Token);
            }
            else
            {
                SetState(StoryPlayerState.PlayingPage);
                _ = PlayFromCurrentPageAsync(playbackCts.Token);
            }
        }

        public void Stop()
        {
            playbackCts?.Cancel();
            playbackCts?.Dispose();
            playbackCts = null;

            inputWaitTcs?.TrySetCanceled();
            inputWaitTcs = null;

            transitionController?.SkipCurrentTransition();
            pageRenderer?.SkipCurrentAnimation();
            pageRenderer?.SetSequenceFont(null);
            pageRenderer?.SetPlaybackSpeed(1f);
            eventSystem?.StopEventTracking();
            audioService?.StopBgm();

            currentSequence = null;
            currentPageIndex = -1;
            isSkipping = false;

            SetState(StoryPlayerState.Idle);
        }

        public void SkipSequence()
        {
            if (!CanSkip || currentState == StoryPlayerState.Idle || currentState == StoryPlayerState.Completed)
            {
                return;
            }

            isSkipping = true;
            playbackSpeed = fastForwardSpeed;
            pageRenderer?.SetPlaybackSpeed(fastForwardSpeed);

            inputWaitTcs?.TrySetResult(true);

            if (currentState == StoryPlayerState.WaitingInput)
            {
                _ = AdvanceToNextPageAsync(playbackCts?.Token ?? default);
            }

            OnSequenceSkipped?.Invoke();
        }

        public void NextPage()
        {
            if (currentState == StoryPlayerState.WaitingInput)
            {
                inputWaitTcs?.TrySetResult(true);
                return;
            }

            if (currentState == StoryPlayerState.PlayingPage && CanSkip)
            {
                var page = GetCurrentPage();
                if (page != null && page.IsVideoPage)
                {
                    pageRenderer?.SkipCurrentAnimation();
                }
            }
        }

        private async Task PlaySequenceAsync(CancellationToken ct)
        {
            try
            {
                while (currentPageIndex < currentSequence.PageCount - 1)
                {
                    ct.ThrowIfCancellationRequested();
                    await AdvanceToNextPageAsync(ct);
                }

                await CompleteSequenceAsync();
            }
            catch (OperationCanceledException)
            {
                Debug.Log("[StoryPlayer] Playback cancelled.");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[StoryPlayer] Playback error: {ex.Message}");
                Stop();
            }
        }

        private async Task PlayFromCurrentPageAsync(CancellationToken ct)
        {
            try
            {
                while (currentPageIndex < currentSequence.PageCount - 1)
                {
                    ct.ThrowIfCancellationRequested();
                    await AdvanceToNextPageAsync(ct);
                }

                await CompleteSequenceAsync();
            }
            catch (OperationCanceledException)
            {
                Debug.Log("[StoryPlayer] Playback cancelled.");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[StoryPlayer] Playback error: {ex.Message}");
                Stop();
            }
        }

        private async Task AdvanceToNextPageAsync(CancellationToken ct)
        {
            var previousPage = currentPageIndex >= 0 ? GetCurrentPage() : null;
            currentPageIndex++;

            if (!currentSequence.TryGetPage(currentPageIndex, out var nextPage))
            {
                return;
            }

            OnPageChanged?.Invoke(currentPageIndex);

            if (previousPage != null)
            {
                SetState(StoryPlayerState.Transitioning);
                await transitionController.PlayTransitionAsync(
                    previousPage.TransitionOut,
                    previousPage.TransitionDuration / playbackSpeed,
                    false);

                eventSystem?.StopEventTracking();
            }

            // Screen is black — prepare new page content instantly before revealing
            pageRenderer.PrepareBackground(nextPage);

            SetState(StoryPlayerState.Transitioning);
            await transitionController.PlayTransitionAsync(
                nextPage.TransitionIn,
                nextPage.TransitionDuration / playbackSpeed,
                true);

            SetState(StoryPlayerState.PlayingPage);

            audioService?.ApplyPageAudioConfig(nextPage.AudioConfig);
            eventSystem?.StartEventTracking(nextPage);

            await pageRenderer.RenderPageAsync(nextPage);

            if (isSkipping)
            {
                return;
            }

            // 视频页：RenderVideoAsync 已经等到视频播完，不再额外等待
            if (nextPage.IsVideoPage && !nextPage.LoopVideo)
            {
                return;
            }

            var playbackMode = currentSequence.DefaultPlaybackMode;
            bool shouldAutoAdvance;
            float autoAdvanceDelay;

            if (playbackMode == StoryPlaybackMode.ClickToAdvance)
            {
                shouldAutoAdvance = nextPage.DisplayDuration > 0f;
                autoAdvanceDelay = nextPage.DisplayDuration;
            }
            else
            {
                shouldAutoAdvance = !nextPage.WaitForInput;
                autoAdvanceDelay = nextPage.DisplayDuration > 0f
                    ? nextPage.DisplayDuration
                    : currentSequence.AutoAdvanceDelay;
            }

            if (shouldAutoAdvance)
            {
                await Task.Delay(TimeSpan.FromSeconds(autoAdvanceDelay / playbackSpeed), ct);
            }
            else
            {
                SetState(StoryPlayerState.WaitingInput);
                await WaitForInputAsync(ct);
            }
        }

        private async Task WaitForInputAsync(CancellationToken ct)
        {
            inputWaitTcs = new TaskCompletionSource<bool>();

            using (ct.Register(() => inputWaitTcs.TrySetCanceled()))
            {
                try
                {
                    await inputWaitTcs.Task;
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
            }

            inputWaitTcs = null;
        }

        private async Task CompleteSequenceAsync()
        {
            SetState(StoryPlayerState.Completed);

            eventSystem?.StopEventTracking();

            var finalPage = GetCurrentPage();
            if (finalPage != null)
            {
                await pageRenderer.HidePageAsync(finalPage, finalPage.TransitionOut, finalPage.TransitionDuration);
            }

            OnSequenceCompleted?.Invoke();
        }

        private void HandleRenderComplete()
        {
            if (isSkipping)
            {
                inputWaitTcs?.TrySetResult(true);
            }
        }

        private void HandleInputRequested()
        {
            if (currentState == StoryPlayerState.WaitingInput)
            {
                return;
            }

            if (!isSkipping)
            {
                SetState(StoryPlayerState.WaitingInput);
            }
        }

        private void SetState(StoryPlayerState newState)
        {
            if (currentState == newState)
            {
                return;
            }

            currentState = newState;
            OnStateChanged?.Invoke(newState);

            Debug.Log($"[StoryPlayer] State changed to: {newState}");
        }

        private StoryPage GetCurrentPage()
        {
            if (currentSequence != null && currentPageIndex >= 0)
            {
                currentSequence.TryGetPage(currentPageIndex, out var page);
                return page;
            }
            return null;
        }

        public void TriggerPageEvent(StoryPageEvent pageEvent)
        {
            OnPageEvent?.Invoke(pageEvent);
        }
    }
}
