using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GameCreate3.StoryPlayer
{
    public sealed class StoryPageRenderer : MonoBehaviour, IStoryPageRenderer
    {
        [Header("Background")]
        [SerializeField] private Image backgroundImage;
        [SerializeField] private Image cgImage;
        [SerializeField] private RectTransform backgroundContainer;

        [Header("Foreground")]
        [SerializeField] private Image foregroundImage;
        [SerializeField] private RectTransform foregroundContainer;
        [SerializeField] private CanvasGroup overlayGroup;

        [Header("Text")]
        [SerializeField] private TMP_Text speakerLabel;
        [SerializeField] private TMP_Text contentLabel;
        [SerializeField] private RectTransform textContainer;
        [SerializeField] private CanvasGroup textGroup;

        [Header("Effects")]
        [SerializeField] private CanvasGroup fadeOverlay;
        [SerializeField] private RectTransform effectContainer;

        [Header("Settings")]
        [SerializeField] private float defaultTypewriterInterval = 0.05f;
        [SerializeField] private float fadeDuration = 0.5f;

        private bool isReady;
        private bool isRendering;
        private CancellationTokenSource renderCts;
        private float playbackSpeed = 1f;
        private int currentTextBlockIndex;
        private List<StoryTextBlock> currentTextBlocks;
        private bool isTypewriterPlaying;
        private string currentFullText;
        private int currentCharIndex;

        public bool IsReady => isReady;
        public bool IsRendering => isRendering;

        public event Action OnRenderComplete;
        public event Action OnInputRequested;

        private void Awake()
        {
            Initialize();
        }

        private void OnDestroy()
        {
            Cleanup();
        }

        public void Initialize()
        {
            isReady = true;
            isRendering = false;
            playbackSpeed = 1f;

            ClearAllContent();
            SetAllAlpha(0f);
        }

        public void Cleanup()
        {
            renderCts?.Cancel();
            renderCts?.Dispose();
            renderCts = null;

            isReady = false;
            isRendering = false;
        }

        public async Task RenderPageAsync(StoryPage page)
        {
            if (!isReady)
            {
                Debug.LogError("[StoryPageRenderer] Not initialized.");
                return;
            }

            renderCts?.Cancel();
            renderCts?.Dispose();
            renderCts = new CancellationTokenSource();

            isRendering = true;
            currentTextBlockIndex = 0;

            try
            {
                await RenderBackgroundAsync(page, renderCts.Token);
                await RenderForegroundAsync(page, renderCts.Token);
                await RenderTextBlocksAsync(page, renderCts.Token);
                await RenderElementsAsync(page, renderCts.Token);

                OnRenderComplete?.Invoke();
            }
            catch (OperationCanceledException)
            {
                Debug.Log("[StoryPageRenderer] Render cancelled.");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[StoryPageRenderer] Render error: {ex.Message}");
            }
        }

        public async Task HidePageAsync(StoryPage page, StoryTransitionType transitionType, float duration)
        {
            if (!isReady)
            {
                return;
            }

            renderCts?.Cancel();

            await PlayTransitionAsync(transitionType, duration, false);
            ClearAllContent();
            SetAllAlpha(0f);

            isRendering = false;
        }

        public void RequestInput()
        {
            if (isTypewriterPlaying)
            {
                SkipCurrentAnimation();
                return;
            }

            if (currentTextBlockIndex < currentTextBlocks?.Count - 1)
            {
                _ = ShowNextTextBlockAsync();
            }
            else
            {
                OnInputRequested?.Invoke();
            }
        }

        public void SkipCurrentAnimation()
        {
            if (isTypewriterPlaying && contentLabel != null)
            {
                contentLabel.text = currentFullText;
                isTypewriterPlaying = false;
                currentCharIndex = currentFullText?.Length ?? 0;
            }
        }

        public void SetPlaybackSpeed(float speed)
        {
            playbackSpeed = Mathf.Max(0.1f, speed);
        }

        private async Task RenderBackgroundAsync(StoryPage page, CancellationToken ct)
        {
            if (backgroundImage != null)
            {
                backgroundImage.sprite = page.BackgroundImage;
                backgroundImage.color = page.BackgroundImage != null ? Color.white : Color.black;
            }

            if (cgImage != null && page.PageType == StoryPageType.CG)
            {
                cgImage.sprite = page.BackgroundImage;
                cgImage.color = Color.white;
            }

            await FadeInAsync(backgroundContainer, page.TransitionDuration, ct);
        }

        private async Task RenderForegroundAsync(StoryPage page, CancellationToken ct)
        {
            if (foregroundImage != null)
            {
                foregroundImage.sprite = page.ForegroundImage;
                foregroundImage.color = page.ForegroundImage != null ? Color.white : Color.clear;
            }

            if (page.ForegroundImage != null)
            {
                await FadeInAsync(foregroundContainer, page.TransitionDuration, ct);
            }
        }

        private async Task RenderTextBlocksAsync(StoryPage page, CancellationToken ct)
        {
            currentTextBlocks = new List<StoryTextBlock>(page.TextBlocks);

            if (currentTextBlocks.Count == 0)
            {
                return;
            }

            await ShowTextBlockAsync(currentTextBlocks[0], ct);
        }

        private async Task ShowTextBlockAsync(StoryTextBlock textBlock, CancellationToken ct)
        {
            if (textBlock.DelayBeforeShow > 0)
            {
                await Task.Delay(TimeSpan.FromSeconds(textBlock.DelayBeforeShow / playbackSpeed), ct);
            }

            if (speakerLabel != null)
            {
                speakerLabel.text = textBlock.Speaker;
            }

            currentFullText = textBlock.Content;
            currentCharIndex = 0;

            if (contentLabel != null)
            {
                switch (textBlock.DisplayMode)
                {
                    case TextDisplayMode.Instant:
                        contentLabel.text = currentFullText;
                        break;

                    case TextDisplayMode.Typewriter:
                        await PlayTypewriterAsync(textBlock, ct);
                        break;

                    case TextDisplayMode.FadeIn:
                        contentLabel.text = currentFullText;
                        await FadeInAsync(textContainer, textBlock.TypewriterSpeed, ct);
                        break;
                }
            }

            if (textBlock.Duration > 0)
            {
                await Task.Delay(TimeSpan.FromSeconds(textBlock.Duration / playbackSpeed), ct);
            }
        }

        private async Task PlayTypewriterAsync(StoryTextBlock textBlock, CancellationToken ct)
        {
            isTypewriterPlaying = true;
            contentLabel.text = string.Empty;

            var interval = textBlock.TypewriterSpeed > 0 ? textBlock.TypewriterSpeed : defaultTypewriterInterval;
            interval /= playbackSpeed;

            for (int i = 0; i <= currentFullText.Length; i++)
            {
                ct.ThrowIfCancellationRequested();

                if (!isTypewriterPlaying)
                {
                    contentLabel.text = currentFullText;
                    break;
                }

                contentLabel.text = currentFullText.Substring(0, i);
                currentCharIndex = i;

                if (i < currentFullText.Length)
                {
                    await Task.Delay(TimeSpan.FromSeconds(interval), ct);
                }
            }

            isTypewriterPlaying = false;
        }

        private async Task ShowNextTextBlockAsync()
        {
            currentTextBlockIndex++;

            if (currentTextBlockIndex >= currentTextBlocks?.Count)
            {
                OnInputRequested?.Invoke();
                return;
            }

            await FadeOutAsync(textContainer, 0.2f, renderCts?.Token ?? default);
            await ShowTextBlockAsync(currentTextBlocks[currentTextBlockIndex], renderCts?.Token ?? default);
        }

        private async Task RenderElementsAsync(StoryPage page, CancellationToken ct)
        {
            foreach (var element in page.Elements)
            {
                ct.ThrowIfCancellationRequested();

                if (element.Delay > 0)
                {
                    await Task.Delay(TimeSpan.FromSeconds(element.Delay / playbackSpeed), ct);
                }

                await RenderElementAsync(element, ct);
            }
        }

        private async Task RenderElementAsync(StoryElement element, CancellationToken ct)
        {
            switch (element.ElementType)
            {
                case StoryElementType.Background:
                    if (backgroundImage != null)
                    {
                        backgroundImage.sprite = element.Image;
                        await PlayElementAnimationAsync(backgroundImage, element, ct);
                    }
                    break;

                case StoryElementType.Character:
                    if (foregroundImage != null)
                    {
                        foregroundImage.sprite = element.Image;
                        await PlayElementAnimationAsync(foregroundImage, element, ct);
                    }
                    break;

                case StoryElementType.DialogueText:
                case StoryElementType.NarrationText:
                    if (contentLabel != null && !string.IsNullOrEmpty(element.Text))
                    {
                        contentLabel.text = element.Text;
                        await PlayElementAnimationAsync(contentLabel, element, ct);
                    }
                    break;

                case StoryElementType.Audio:
                    if (element.AudioClip != null)
                    {
                        var audioSource = GetComponent<AudioSource>() ?? gameObject.AddComponent<AudioSource>();
                        audioSource.PlayOneShot(element.AudioClip);
                    }
                    break;
            }
        }

        private async Task PlayElementAnimationAsync(Graphic graphic, StoryElement element, CancellationToken ct)
        {
            if (element.AnimationType == StoryAnimationType.None)
            {
                return;
            }

            var duration = element.AnimationDuration / playbackSpeed;

            switch (element.AnimationType)
            {
                case StoryAnimationType.FadeIn:
                    await FadeInAsync(graphic.rectTransform, duration, ct);
                    break;

                case StoryAnimationType.SlideIn:
                    await SlideInAsync(graphic.rectTransform, duration, ct);
                    break;

                case StoryAnimationType.ScaleIn:
                    await ScaleInAsync(graphic.rectTransform, duration, ct);
                    break;
            }
        }

        private async Task PlayTransitionAsync(StoryTransitionType transitionType, float duration, bool isIn)
        {
            if (fadeOverlay == null)
            {
                return;
            }

            var targetAlpha = isIn ? 0f : 1f;
            var startAlpha = isIn ? 1f : 0f;

            switch (transitionType)
            {
                case StoryTransitionType.None:
                    fadeOverlay.alpha = targetAlpha;
                    break;

                case StoryTransitionType.Fade:
                case StoryTransitionType.CrossFade:
                    await AnimateFloatAsync(startAlpha, targetAlpha, duration, alpha => fadeOverlay.alpha = alpha, default);
                    break;

                default:
                    fadeOverlay.alpha = targetAlpha;
                    break;
            }
        }

        private async Task FadeInAsync(RectTransform target, float duration, CancellationToken ct)
        {
            var canvasGroup = target.GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                canvasGroup = target.gameObject.AddComponent<CanvasGroup>();
            }

            await AnimateFloatAsync(0f, 1f, duration / playbackSpeed,
                alpha => canvasGroup.alpha = alpha,
                ct);
        }

        private async Task FadeOutAsync(RectTransform target, float duration, CancellationToken ct)
        {
            var canvasGroup = target.GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                return;
            }

            await AnimateFloatAsync(1f, 0f, duration / playbackSpeed,
                alpha => canvasGroup.alpha = alpha,
                ct);
        }

        private async Task SlideInAsync(RectTransform target, float duration, CancellationToken ct)
        {
            var startPos = target.anchoredPosition;
            startPos.x += 100f;
            target.anchoredPosition = startPos;

            await AnimateFloatAsync(0f, 1f, duration / playbackSpeed,
                t =>
                {
                    var pos = target.anchoredPosition;
                    pos.x = Mathf.Lerp(startPos.x, startPos.x - 100f, t);
                    target.anchoredPosition = pos;
                },
                ct);
        }

        private async Task ScaleInAsync(RectTransform target, float duration, CancellationToken ct)
        {
            await AnimateFloatAsync(0f, 1f, duration / playbackSpeed,
                t => target.localScale = Vector3.one * t,
                ct);
        }

        private async Task AnimateFloatAsync(float from, float to, float duration, Action<float> setter, CancellationToken ct)
        {
            if (duration <= 0)
            {
                setter(to);
                return;
            }

            var elapsed = 0f;

            while (elapsed < duration)
            {
                ct.ThrowIfCancellationRequested();

                elapsed += Time.deltaTime;
                var t = Mathf.Clamp01(elapsed / duration);
                setter(Mathf.Lerp(from, to, t));

                await Task.Yield();
            }

            setter(to);
        }

        private void ClearAllContent()
        {
            if (backgroundImage != null)
            {
                backgroundImage.sprite = null;
                backgroundImage.color = Color.black;
            }

            if (cgImage != null)
            {
                cgImage.sprite = null;
                cgImage.color = Color.clear;
            }

            if (foregroundImage != null)
            {
                foregroundImage.sprite = null;
                foregroundImage.color = Color.clear;
            }

            if (speakerLabel != null)
            {
                speakerLabel.text = string.Empty;
            }

            if (contentLabel != null)
            {
                contentLabel.text = string.Empty;
            }

            currentTextBlocks?.Clear();
            currentTextBlockIndex = 0;
            isTypewriterPlaying = false;
        }

        private void SetAllAlpha(float alpha)
        {
            if (overlayGroup != null)
            {
                overlayGroup.alpha = alpha;
            }

            if (textGroup != null)
            {
                textGroup.alpha = alpha;
            }

            if (fadeOverlay != null)
            {
                fadeOverlay.alpha = alpha;
            }
        }
    }
}
