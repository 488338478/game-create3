using System;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace GameCreate3.StoryPlayer
{
    public sealed class SimpleTransitionController : MonoBehaviour, ITransitionController
    {
        [Header("Components")]
        [SerializeField] private Canvas transitionCanvas;
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private Image transitionImage;
        [SerializeField] private RectTransform transitionRect;

        [Header("Settings")]
        [SerializeField] private bool useUnscaledTime = true;

        private bool isTransitioning;
        private CancellationTokenSource transitionCts;
        private float currentProgress;

        public bool IsTransitioning => isTransitioning;

        private void Awake()
        {
            Initialize();
        }

        private void OnDestroy()
        {
            Cleanup();
        }

        private void Initialize()
        {
            if (transitionCanvas == null)
            {
                transitionCanvas = GetComponent<Canvas>();
            }

            if (canvasGroup == null)
            {
                canvasGroup = GetComponent<CanvasGroup>();
            }

            if (transitionRect == null)
            {
                transitionRect = GetComponent<RectTransform>();
            }

            if (transitionCanvas != null)
            {
                transitionCanvas.sortingOrder = 1000;
                transitionCanvas.gameObject.SetActive(false);
            }

            if (canvasGroup != null)
            {
                canvasGroup.alpha = 0f;
            }

            if (transitionRect != null)
            {
                transitionRect.anchorMin = Vector2.zero;
                transitionRect.anchorMax = Vector2.one;
                transitionRect.offsetMin = Vector2.zero;
                transitionRect.offsetMax = Vector2.zero;
            }
        }

        private void Cleanup()
        {
            transitionCts?.Cancel();
            transitionCts?.Dispose();
            transitionCts = null;
        }

        public async Task PlayTransitionAsync(StoryTransitionType transitionType, float duration, bool isIn)
        {
            if (transitionType == StoryTransitionType.None || duration <= 0)
            {
                if (isIn) DisableTransitionCanvas();
                return;
            }

            transitionCts?.Cancel();
            transitionCts?.Dispose();
            transitionCts = new CancellationTokenSource();

            isTransitioning = true;
            currentProgress = 0f;

            try
            {
                if (!isIn)
                {
                    // TransitionOut: start transparent, fade to black
                    EnableTransitionCanvas();
                }
                else
                {
                    // TransitionIn: start from black (left by TransitionOut), fade to transparent
                    if (transitionCanvas != null) transitionCanvas.gameObject.SetActive(true);
                    if (canvasGroup != null) canvasGroup.alpha = 1f;
                }

                SetupTransition(transitionType, isIn);

                await AnimateTransitionAsync(transitionType, duration, isIn, transitionCts.Token);

                if (isIn)
                {
                    DisableTransitionCanvas();
                }
            }
            catch (OperationCanceledException)
            {
                Debug.Log("[SimpleTransitionController] Transition cancelled.");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SimpleTransitionController] Transition error: {ex.Message}");
            }
            finally
            {
                isTransitioning = false;
            }
        }

        public void SkipCurrentTransition()
        {
            if (!isTransitioning)
            {
                return;
            }

            transitionCts?.Cancel();
            currentProgress = 1f;
            DisableTransitionCanvas();
            isTransitioning = false;
        }

        private void EnableTransitionCanvas()
        {
            if (transitionCanvas != null)
            {
                transitionCanvas.gameObject.SetActive(true);
            }

            if (canvasGroup != null)
            {
                canvasGroup.alpha = 0f;
            }
        }

        private void DisableTransitionCanvas()
        {
            if (transitionCanvas != null)
            {
                transitionCanvas.gameObject.SetActive(false);
            }
        }

        private void SetupTransition(StoryTransitionType transitionType, bool isIn)
        {
            if (transitionRect == null)
            {
                return;
            }

            var startPos = Vector2.zero;

            switch (transitionType)
            {
                case StoryTransitionType.SlideLeft:
                    startPos = isIn ? new Vector2(1f, 0f) : Vector2.zero;
                    break;
                case StoryTransitionType.SlideRight:
                    startPos = isIn ? new Vector2(-1f, 0f) : Vector2.zero;
                    break;
                case StoryTransitionType.SlideUp:
                    startPos = isIn ? new Vector2(0f, -1f) : Vector2.zero;
                    break;
                case StoryTransitionType.SlideDown:
                    startPos = isIn ? new Vector2(0f, 1f) : Vector2.zero;
                    break;
                default:
                    startPos = Vector2.zero;
                    break;
            }

            transitionRect.anchoredPosition = new Vector2(
                startPos.x * Screen.width,
                startPos.y * Screen.height
            );
        }

        private async Task AnimateTransitionAsync(StoryTransitionType transitionType, float duration, bool isIn, CancellationToken ct)
        {
            var elapsed = 0f;

            while (elapsed < duration)
            {
                ct.ThrowIfCancellationRequested();

                elapsed += useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
                currentProgress = Mathf.Clamp01(elapsed / duration);

                ApplyTransitionEffect(transitionType, currentProgress, isIn);

                await Task.Yield();
            }

            currentProgress = 1f;
            ApplyTransitionEffect(transitionType, 1f, isIn);
        }

        private void ApplyTransitionEffect(StoryTransitionType transitionType, float progress, bool isIn)
        {
            var effectiveProgress = isIn ? 1f - progress : progress;

            switch (transitionType)
            {
                case StoryTransitionType.Fade:
                case StoryTransitionType.CrossFade:
                    if (canvasGroup != null)
                    {
                        canvasGroup.alpha = effectiveProgress;
                    }
                    break;

                case StoryTransitionType.SlideLeft:
                    ApplySlideTransition(Vector2.left, progress, isIn);
                    break;

                case StoryTransitionType.SlideRight:
                    ApplySlideTransition(Vector2.right, progress, isIn);
                    break;

                case StoryTransitionType.SlideUp:
                    ApplySlideTransition(Vector2.up, progress, isIn);
                    break;

                case StoryTransitionType.SlideDown:
                    ApplySlideTransition(Vector2.down, progress, isIn);
                    break;

                case StoryTransitionType.Scale:
                    if (transitionRect != null)
                    {
                        var scale = Mathf.Lerp(0f, 1f, effectiveProgress);
                        transitionRect.localScale = Vector3.one * scale;
                    }
                    if (canvasGroup != null)
                    {
                        canvasGroup.alpha = effectiveProgress;
                    }
                    break;

                default:
                    if (canvasGroup != null)
                    {
                        canvasGroup.alpha = effectiveProgress;
                    }
                    break;
            }
        }

        private void ApplySlideTransition(Vector2 direction, float progress, bool isIn)
        {
            if (transitionRect == null)
            {
                return;
            }

            var startOffset = isIn ? direction : Vector2.zero;
            var endOffset = isIn ? Vector2.zero : -direction;

            var currentOffset = Vector2.Lerp(startOffset, endOffset, progress);
            transitionRect.anchoredPosition = new Vector2(
                currentOffset.x * Screen.width,
                currentOffset.y * Screen.height
            );

            if (canvasGroup != null)
            {
                canvasGroup.alpha = 1f;
            }
        }

        public void SetTransitionColor(Color color)
        {
            if (transitionImage != null)
            {
                transitionImage.color = color;
            }
        }
    }
}
