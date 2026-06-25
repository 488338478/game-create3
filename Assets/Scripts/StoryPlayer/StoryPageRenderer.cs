using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

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

        [Header("Video")]
        [SerializeField] private VideoPlayer videoPlayer;
        [SerializeField] private RawImage videoRawImage;

        [Header("Effects")]
        [SerializeField] private CanvasGroup fadeOverlay;
        [SerializeField] private RectTransform effectContainer;

        [Header("Settings")]
        [SerializeField] private float defaultTypewriterInterval = 0.05f;
        [SerializeField] private float fadeDuration = 0.5f;

        private bool isReady;
        private bool isRendering;
        private bool isVideoPlaying;
        private bool videoErrorOccurred;
        private CancellationTokenSource renderCts;
        private float playbackSpeed = 1f;
        private int currentTextBlockIndex;
        private List<StoryTextBlock> currentTextBlocks;
        private bool isTypewriterPlaying;
        private string currentFullText;
        private int currentCharIndex;
        private RectTransformState defaultTextContainerState;
        private TextStyleState defaultSpeakerStyle;
        private TextStyleState defaultContentStyle;
        private TMP_FontAsset activeSequenceFont;
        private IAudioService audioService;

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

            CaptureDefaultTextState();
            ClearAllContent();
            SetAllAlpha(0f);
        }

        public void SetAudioService(IAudioService audio)
        {
            audioService = audio;
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
            EnsureDefaultTextStateCaptured();
            ClearTextContent();

            try
            {
                if (page.IsVideoPage)
                {
                    await RenderVideoAsync(page, renderCts.Token);
                }
                else
                {
                    StopVideo();
                    await RenderBackgroundAsync(page, renderCts.Token);
                    await RenderForegroundAsync(page, renderCts.Token);
                }

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
            finally
            {
                isRendering = false;
            }
        }

        public void PrepareBackground(StoryPage page)
        {
            if (!page.IsVideoPage)
            {
                StopVideo();
            }

            ClearTextContent();

            if (backgroundImage != null)
            {
                backgroundImage.sprite = page.BackgroundImage;
                backgroundImage.color = page.BackgroundImage != null ? Color.white : Color.black;
            }

            if (backgroundContainer != null)
            {
                var cg = backgroundContainer.GetComponent<CanvasGroup>();
                if (cg == null) cg = backgroundContainer.gameObject.AddComponent<CanvasGroup>();
                cg.alpha = 1f;
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

        public bool RequestInput()
        {
            if (isVideoPlaying)
            {
                return true;
            }

            if (isTypewriterPlaying)
            {
                SkipCurrentAnimation();
                return true;
            }

            if (currentTextBlockIndex < currentTextBlocks?.Count - 1)
            {
                _ = ShowNextTextBlockAsync();
                return true;
            }

            OnInputRequested?.Invoke();
            return false;
        }

        private void SkipVideo()
        {
            isVideoPlaying = false;
            StopVideo();
        }

        public void SkipCurrentAnimation()
        {
            if (isVideoPlaying)
            {
                SkipVideo();
                return;
            }

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

        public void SetSequenceFont(TMP_FontAsset fontAsset)
        {
            EnsureDefaultTextStateCaptured();
            activeSequenceFont = fontAsset;
            ApplyActiveSequenceFont();
        }

        private async Task RenderVideoAsync(StoryPage page, CancellationToken ct)
        {
            if (videoPlayer == null || videoRawImage == null || page.VideoClip == null)
            {
                return;
            }

            videoPlayer.Stop();
            videoPlayer.clip = page.VideoClip;
            videoPlayer.isLooping = page.LoopVideo;
            videoPlayer.playbackSpeed = page.VideoPlaybackSpeed * playbackSpeed;

            videoPlayer.errorReceived += OnVideoError;
            videoErrorOccurred = false;

            videoPlayer.Prepare();

            var prepareTimeout = 5f;
            var elapsed = 0f;
            while (!videoPlayer.isPrepared && !videoErrorOccurred)
            {
                ct.ThrowIfCancellationRequested();
                elapsed += Time.deltaTime;
                if (elapsed > prepareTimeout)
                {
                    Debug.LogWarning($"[StoryPageRenderer] VideoPlayer prepare timeout: {page.VideoClip.name}");
                    videoPlayer.errorReceived -= OnVideoError;
                    return;
                }
                await Task.Yield();
            }

            if (videoErrorOccurred)
            {
                videoPlayer.errorReceived -= OnVideoError;
                return;
            }

            var renderTexture = videoPlayer.targetTexture;
            if (renderTexture == null || renderTexture.width != (int)page.VideoClip.width
                || renderTexture.height != (int)page.VideoClip.height)
            {
                if (renderTexture != null) renderTexture.Release();
                renderTexture = new RenderTexture(
                    (int)page.VideoClip.width,
                    (int)page.VideoClip.height,
                    0);
                videoPlayer.targetTexture = renderTexture;
            }

            videoRawImage.texture = renderTexture;
            videoRawImage.enabled = true;
            isVideoPlaying = true;

            if (backgroundImage != null)
            {
                backgroundImage.color = Color.clear;
            }

            videoPlayer.Play();

            if (!page.LoopVideo)
            {
                // Wait for VideoPlayer to actually start (up to 1 second)
                var startTimeout = 1f;
                elapsed = 0f;
                while (!videoPlayer.isPlaying && isVideoPlaying && !videoErrorOccurred)
                {
                    ct.ThrowIfCancellationRequested();
                    elapsed += Time.deltaTime;
                    if (elapsed > startTimeout) break;
                    await Task.Yield();
                }

                // Wait for playback to finish
                while (videoPlayer.isPlaying && isVideoPlaying && !videoErrorOccurred)
                {
                    ct.ThrowIfCancellationRequested();
                    await Task.Yield();
                }

                if (videoRawImage != null)
                {
                    videoRawImage.enabled = false;
                }
            }

            videoPlayer.errorReceived -= OnVideoError;
            isVideoPlaying = false;
        }

        private void OnVideoError(VideoPlayer source, string message)
        {
            Debug.LogError($"[StoryPageRenderer] VideoPlayer error: {message}");
            videoErrorOccurred = true;
            isVideoPlaying = false;
        }

        private void StopVideo()
        {
            isVideoPlaying = false;

            if (videoPlayer != null)
            {
                videoPlayer.Stop();
                videoPlayer.clip = null;
            }

            if (videoRawImage != null)
            {
                videoRawImage.texture = null;
                videoRawImage.enabled = false;
            }
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

            if (backgroundContainer != null)
            {
                var cg = backgroundContainer.GetComponent<CanvasGroup>();
                // Skip fade-in if PrepareBackground already made it visible
                if (cg == null || cg.alpha < 1f)
                    await FadeInAsync(backgroundContainer, page.TransitionDuration, ct);
            }
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
                if (foregroundContainer != null)
                {
                    await FadeInAsync(foregroundContainer, page.TransitionDuration, ct);
                }
            }
        }

        private async Task RenderTextBlocksAsync(StoryPage page, CancellationToken ct)
        {
            currentTextBlocks = new List<StoryTextBlock>(page.TextBlocks);

            if (currentTextBlocks.Count == 0)
            {
                ClearTextContent();
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

            ApplyTextBlockLayout(textBlock);
            ApplyTextBlockStyles(textBlock);
            ApplyActiveSequenceFont();
            SetTextVisible(true);

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
                        if (textContainer != null)
                        {
                            await FadeInAsync(textContainer, textBlock.TypewriterSpeed, ct);
                        }
                        break;
                }
            }

            if (textBlock.Duration > 0)
            {
                await Task.Delay(TimeSpan.FromSeconds(textBlock.Duration / playbackSpeed), ct);
            }
        }

        private void ApplyTextBlockLayout(StoryTextBlock textBlock)
        {
            if (textContainer == null)
            {
                return;
            }

            if (textBlock.OverrideTextContainer)
            {
                textContainer.anchorMin = textBlock.TextAnchorMin;
                textContainer.anchorMax = textBlock.TextAnchorMax;
                textContainer.pivot = textBlock.TextPivot;
                textContainer.anchoredPosition = textBlock.TextAnchoredPosition;
                textContainer.sizeDelta = textBlock.TextSizeDelta;
            }
            else
            {
                ApplyRectTransformState(textContainer, defaultTextContainerState);
            }
        }

        private void ApplyTextBlockStyles(StoryTextBlock textBlock)
        {
            if (contentLabel != null)
            {
                if (ShouldApplyContentStyle(textBlock))
                {
                    if (textBlock.ContentFontSize > 0f)
                    {
                        contentLabel.fontSize = textBlock.ContentFontSize;
                    }

                    if (textBlock.ContentColor.a > 0f)
                    {
                        contentLabel.color = textBlock.ContentColor;
                    }

                    contentLabel.alignment = textBlock.ContentAlignment;
                }
                else
                {
                    ApplyTextStyle(contentLabel, defaultContentStyle);
                }
            }

            if (speakerLabel != null)
            {
                if (ShouldApplySpeakerStyle(textBlock))
                {
                    if (textBlock.SpeakerFontSize > 0f)
                    {
                        speakerLabel.fontSize = textBlock.SpeakerFontSize;
                    }

                    if (textBlock.SpeakerColor.a > 0f)
                    {
                        speakerLabel.color = textBlock.SpeakerColor;
                    }

                    speakerLabel.alignment = textBlock.SpeakerAlignment;
                }
                else
                {
                    ApplyTextStyle(speakerLabel, defaultSpeakerStyle);
                }
            }
        }

        private static bool ShouldApplyContentStyle(StoryTextBlock textBlock)
        {
            return textBlock.OverrideContentStyle
                || textBlock.ContentFontSize > 0f
                || textBlock.ContentColor.a > 0f;
        }

        private static bool ShouldApplySpeakerStyle(StoryTextBlock textBlock)
        {
            return textBlock.OverrideSpeakerStyle
                || textBlock.SpeakerFontSize > 0f
                || textBlock.SpeakerColor.a > 0f;
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

            if (textContainer != null)
            {
                await FadeOutAsync(textContainer, 0.2f, renderCts?.Token ?? default);
            }
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
                    if (backgroundImage != null && element.Image != null)
                    {
                        backgroundImage.sprite = element.Image;
                        await PlayElementAnimationAsync(backgroundImage, element, ct);
                    }
                    break;

                case StoryElementType.Character:
                    if (foregroundImage != null && element.Image != null)
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
                        audioService?.PlaySfx(element.AudioClip);
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
            StopVideo();

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

            ClearTextContent();

            currentTextBlocks?.Clear();
            currentTextBlockIndex = 0;
            isTypewriterPlaying = false;
        }

        private void ClearTextContent()
        {
            EnsureDefaultTextStateCaptured();

            if (speakerLabel != null)
            {
                speakerLabel.text = string.Empty;
                ApplyTextStyle(speakerLabel, defaultSpeakerStyle);
            }

            if (contentLabel != null)
            {
                contentLabel.text = string.Empty;
                ApplyTextStyle(contentLabel, defaultContentStyle);
            }

            if (textContainer != null)
            {
                ApplyRectTransformState(textContainer, defaultTextContainerState);
            }

            isTypewriterPlaying = false;
            currentFullText = string.Empty;
            currentCharIndex = 0;
        }

        private void CaptureDefaultTextState()
        {
            defaultTextContainerState = RectTransformState.Capture(textContainer);
            defaultSpeakerStyle = TextStyleState.Capture(speakerLabel);
            defaultContentStyle = TextStyleState.Capture(contentLabel);
        }

        private void ApplyActiveSequenceFont()
        {
            if (speakerLabel != null)
            {
                ApplyFontAsset(speakerLabel, activeSequenceFont != null ? activeSequenceFont : defaultSpeakerStyle.Font);
            }

            if (contentLabel != null)
            {
                ApplyFontAsset(contentLabel, activeSequenceFont != null ? activeSequenceFont : defaultContentStyle.Font);
            }
        }

        private void EnsureDefaultTextStateCaptured()
        {
            if (!defaultTextContainerState.IsValid && textContainer != null)
            {
                defaultTextContainerState = RectTransformState.Capture(textContainer);
            }

            if (!defaultSpeakerStyle.IsValid && speakerLabel != null)
            {
                defaultSpeakerStyle = TextStyleState.Capture(speakerLabel);
            }

            if (!defaultContentStyle.IsValid && contentLabel != null)
            {
                defaultContentStyle = TextStyleState.Capture(contentLabel);
            }
        }

        private static void ApplyRectTransformState(RectTransform target, RectTransformState state)
        {
            if (target == null || !state.IsValid)
            {
                return;
            }

            target.anchorMin = state.AnchorMin;
            target.anchorMax = state.AnchorMax;
            target.pivot = state.Pivot;
            target.anchoredPosition = state.AnchoredPosition;
            target.sizeDelta = state.SizeDelta;
        }

        private static void ApplyTextStyle(TMP_Text target, TextStyleState state)
        {
            if (target == null || !state.IsValid)
            {
                return;
            }

            target.fontSize = state.FontSize;
            target.color = state.Color;
            target.alignment = state.Alignment;
            ApplyFontAsset(target, state.Font);
        }

        private static void ApplyFontAsset(TMP_Text target, TMP_FontAsset fontAsset)
        {
            if (target == null || fontAsset == null)
            {
                return;
            }

            target.font = fontAsset;
            target.fontSharedMaterial = fontAsset.material;
            target.ForceMeshUpdate(true, true);
        }

        private void SetTextVisible(bool visible)
        {
            var alpha = visible ? 1f : 0f;

            if (textGroup != null)
            {
                textGroup.alpha = alpha;
            }

            if (textContainer != null)
            {
                var canvasGroup = textContainer.GetComponent<CanvasGroup>();
                if (canvasGroup != null)
                {
                    canvasGroup.alpha = alpha;
                }
            }
        }

        private readonly struct RectTransformState
        {
            public readonly bool IsValid;
            public readonly Vector2 AnchorMin;
            public readonly Vector2 AnchorMax;
            public readonly Vector2 Pivot;
            public readonly Vector2 AnchoredPosition;
            public readonly Vector2 SizeDelta;

            private RectTransformState(RectTransform rectTransform)
            {
                IsValid = rectTransform != null;
                AnchorMin = rectTransform != null ? rectTransform.anchorMin : Vector2.zero;
                AnchorMax = rectTransform != null ? rectTransform.anchorMax : Vector2.one;
                Pivot = rectTransform != null ? rectTransform.pivot : new Vector2(0.5f, 0.5f);
                AnchoredPosition = rectTransform != null ? rectTransform.anchoredPosition : Vector2.zero;
                SizeDelta = rectTransform != null ? rectTransform.sizeDelta : Vector2.zero;
            }

            public static RectTransformState Capture(RectTransform rectTransform)
            {
                return new RectTransformState(rectTransform);
            }
        }

        private readonly struct TextStyleState
        {
            public readonly bool IsValid;
            public readonly float FontSize;
            public readonly Color Color;
            public readonly TextAlignmentOptions Alignment;
            public readonly TMP_FontAsset Font;

            private TextStyleState(TMP_Text text)
            {
                IsValid = text != null;
                FontSize = text != null ? text.fontSize : 0f;
                Color = text != null ? text.color : Color.white;
                Alignment = text != null ? text.alignment : TextAlignmentOptions.TopLeft;
                Font = text != null ? text.font : null;
            }

            public static TextStyleState Capture(TMP_Text text)
            {
                return new TextStyleState(text);
            }
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
