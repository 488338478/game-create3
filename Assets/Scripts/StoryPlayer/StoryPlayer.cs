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
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private StoryAudioAdapter audioAdapter;
        [SerializeField] private StoryEventSystem eventSystem;

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

        private void Awake()
        {
            // Prefab 化 fallback —— 如果 7 个剧情组件都挂在同一 GameObject 上（rig prefab 模式），
            // Awake 时自动 GetComponent 把 IStoryPageRenderer / ITransitionController 兜起来，
            // 不需要外部 Bootstrap 串 Initialize 链。
            if (initialized) return;
            if (pageRenderer == null) pageRenderer = GetComponent<IStoryPageRenderer>();
            if (transitionController == null) transitionController = GetComponent<ITransitionController>();
            if (pageRenderer != null && transitionController != null)
            {
                Initialize(pageRenderer, transitionController);
            }
        }

        public void Initialize(IStoryPageRenderer renderer, ITransitionController transition)
        {
            if (initialized) return;
            initialized = true;
            pageRenderer = renderer ?? throw new ArgumentNullException(nameof(renderer));
            transitionController = transition ?? throw new ArgumentNullException(nameof(transition));

            pageRenderer.OnRenderComplete += HandleRenderComplete;
            pageRenderer.OnInputRequested += HandleInputRequested;

            if (audioAdapter == null)
            {
                audioAdapter = GetComponent<StoryAudioAdapter>();
            }

            if (eventSystem == null)
            {
                eventSystem = GetComponent<StoryEventSystem>();
            }

            if (audioAdapter != null && eventSystem != null)
            {
                audioAdapter.BindEventSystem(eventSystem);
            }
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
            eventSystem?.StopEventTracking();
            audioAdapter?.StopBgm();

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
            if (currentState != StoryPlayerState.WaitingInput)
            {
                return;
            }

            inputWaitTcs?.TrySetResult(true);
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
            }

            SetState(StoryPlayerState.Transitioning);
            await transitionController.PlayTransitionAsync(
                nextPage.TransitionIn,
                nextPage.TransitionDuration / playbackSpeed,
                true);

            SetState(StoryPlayerState.PlayingPage);

            audioAdapter?.ApplyPageAudioConfig(nextPage.AudioConfig);
            eventSystem?.StartEventTracking(nextPage);

            await pageRenderer.RenderPageAsync(nextPage);

            if (isSkipping)
            {
                return;
            }

            var playbackMode = currentSequence.DefaultPlaybackMode;
            var shouldWaitForInput = nextPage.WaitForInput;
            var autoAdvanceDelay = nextPage.DisplayDuration > 0
                ? nextPage.DisplayDuration
                : currentSequence.AutoAdvanceDelay;

            if (playbackMode == StoryPlaybackMode.AutoAdvance && !shouldWaitForInput)
            {
                await Task.Delay(TimeSpan.FromSeconds(autoAdvanceDelay / playbackSpeed), ct);
            }
            else if (shouldWaitForInput)
            {
                SetState(StoryPlayerState.WaitingInput);
                pageRenderer.RequestInput();
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
