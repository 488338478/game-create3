using System;
using System.Collections;
using System.Collections.Generic;
using GameCreate3.DualWorld;
using GameCreate3.Core.SceneRouting;
using UnityEngine;
using UnityEngine.UI;

namespace GameCreate3
{
    public sealed class ColorPuzzleController : MonoBehaviour
    {
        private readonly string[] defaultRejectLines =
        {
            "写的什么玩意，审美呢？",
            "211就这水平？",
            "你是老太太吗，年轻点！"
        };

        private readonly string[] defaultApproveLines =
        {
            "还得是高材生，可以可以。"
        };

        [SerializeField] private CanvasGroup interactionGroup;
        [SerializeField] private Button submitButton;
        [SerializeField] private BossFeedbackPanel feedbackPanel;
        [SerializeField] private Text hintLabel;
        [SerializeField] private List<ColorSlot> colorSlots = new List<ColorSlot>();
        [SerializeField] private CanvasGroup dreamPaletteGroup;
        [SerializeField] private ColorPaletteSwatch currentPaletteSwatch;
        [SerializeField] private float submitToBossReplyDelaySec = 0.35f;
        [SerializeField] private float successTransitionDelaySec = 1.1f;
        [SerializeField] private string successSceneName = "Level2Cutscene";
        [SerializeField] private List<string> playerSubmitLines = new List<string>
        {
            "老板，我改了一版，您看看。",
            "这次我把颜色重新顺过了，您再看一眼？",
            "我又调了一遍，这版会不会好点？",
            "按您的意思往年轻一点改了，您看看行不行。",
            "我再提一次，您帮我过下。"
        };
        [SerializeField] private bool forceFirstFailure = true;
        [SerializeField] private float disabledAlpha = 0.42f;
        [SerializeField] private string initialBossLine = "排版可以了，写内容吧。";
        [SerializeField] private string initialHintLine = "在左侧梦境中通过跳跃拾取颜色，请在右侧将颜色正确地填充。";
        [SerializeField] private string dreamPaletteHintLine = "接到哪种 ID 的颜色，右侧色板就切到哪一版，再点击对应组件替换成同 ID 的样式。";
        [SerializeField] private string successSummaryFormat = "这版顺眼多了，{0} / {1} 个组件都换对了。";
        [SerializeField] private List<string> rejectLines = new List<string>();
        [SerializeField] private List<string> approveLines = new List<string>();

        private const string RuntimeHintName = "RuntimeHintLabel";
        private const string RuntimeSubmitName = "RuntimeSubmitButton";
        private const string RuntimeSubmitTextName = "RuntimeSubmitText";
        private const string RuntimeFeedbackTextName = "RuntimeFeedbackText";

        private bool dreamPaletteEnabled;
        private bool interactable;
        private bool forcedFailurePending = true;
        private int rejectLineIndex;
        private int approveLineIndex;
        private int playerSubmitIndex;
        private bool hasCurrentPalette;
        private PaletteColorOption currentPaletteOption;
        private ChatTaskController chatTaskController;
        private Coroutine pendingSubmitRoutine;
        private Coroutine pendingSuccessTransitionRoutine;
        private bool successTransitionQueued;

        public event Action<ColorSubmitResult> OnSubmitAttempted;

        public void Initialize(
            CanvasGroup canvasGroup,
            Button submit,
            BossFeedbackPanel bossFeedback,
            Text hint,
            List<ColorSlot> slots)
        {
            interactionGroup = canvasGroup;
            submitButton = submit;
            feedbackPanel = bossFeedback;
            hintLabel = hint;
            colorSlots = slots ?? new List<ColorSlot>();

            foreach (var slot in colorSlots)
            {
                slot.SetDreamPaletteEnabled(false);
                slot.SetInteractable(true);
                slot.Clicked -= HandleColorSlotClicked;
                slot.Clicked += HandleColorSlotClicked;
            }

            if (submitButton != null)
            {
                submitButton.onClick.AddListener(HandleSubmitClicked);
            }
        }

        public void RuntimeConfigure(
            CanvasGroup canvasGroup,
            Button submit,
            BossFeedbackPanel bossFeedback,
            Text hint,
            List<ColorSlot> slots)
        {
            Initialize(canvasGroup, submit, bossFeedback, hint, slots);
        }

        private void Awake()
        {
            if (colorSlots.Count == 0)
            {
                colorSlots.AddRange(GetComponentsInChildren<ColorSlot>(true));
            }

            if (currentPaletteSwatch == null)
            {
                currentPaletteSwatch = GetComponentInChildren<ColorPaletteSwatch>(true);
            }

            if (dreamPaletteGroup == null && currentPaletteSwatch != null)
            {
                dreamPaletteGroup = currentPaletteSwatch.GetComponentInParent<CanvasGroup>();
            }

            EnsureFeedbackLineFallbacks();
            forcedFailurePending = forceFirstFailure;
            AutoConfigureRuntimeUi();
        }

        private void OnEnable()
        {
            if (submitButton != null)
            {
                submitButton.onClick.RemoveListener(HandleSubmitClicked);
                submitButton.onClick.AddListener(HandleSubmitClicked);
            }

            BindRuntimeEvents();
            SetDreamPaletteEnabled(false);
            SetInteractable(true);
        }

        private void OnDisable()
        {
            if (submitButton != null)
            {
                submitButton.onClick.RemoveListener(HandleSubmitClicked);
            }

            CancelPendingSubmit();
            CancelPendingSuccessTransition();
            UnbindRuntimeEvents();
        }

        public void SetDreamPaletteEnabled(bool enabled)
        {
            dreamPaletteEnabled = enabled;

            foreach (var slot in colorSlots)
            {
                slot.SetDreamPaletteEnabled(enabled);
            }

            RefreshDreamPaletteArea();

            if (hintLabel != null)
            {
                hintLabel.text = enabled
                    ? dreamPaletteHintLine
                    : initialHintLine;
            }

        }

        public void SetDreamPaletteColors(IReadOnlyList<PaletteColorOption> options)
        {
            if (options == null || options.Count == 0)
            {
                ClearCurrentPalette();
                return;
            }

            SetCurrentPalette(options[options.Count - 1]);
        }

        public void SetCurrentPalette(PaletteColorOption option)
        {
            currentPaletteOption = option;
            hasCurrentPalette = option.IsValid || option.paletteSprite != null;
            RefreshDreamPaletteArea();
        }

        public void ClearCurrentPalette()
        {
            currentPaletteOption = default;
            hasCurrentPalette = false;
            RefreshDreamPaletteArea();
        }

        public void FlashTargetsForOption(PaletteColorOption option)
        {
            if (!option.IsValid)
            {
                return;
            }

            for (var i = 0; i < colorSlots.Count; i++)
            {
                var slot = colorSlots[i];
                if (slot != null && slot.MatchesOption(option))
                {
                    slot.PlayHintPulse(option);
                }
            }
        }

        public bool TryResolveBlockIndex(PaletteColorOption option, out int blockIndex)
        {
            blockIndex = -1;
            if (!option.IsValid)
            {
                return false;
            }

            for (var i = 0; i < colorSlots.Count; i++)
            {
                var slot = colorSlots[i];
                if (slot == null || !slot.MatchesOption(option))
                {
                    continue;
                }

                if (slot.TryGetBlockIndex(out blockIndex))
                {
                    return true;
                }
            }

            return false;
        }

        public void SetInteractable(bool enabled)
        {
            interactable = enabled;

            if (interactionGroup != null)
            {
                interactionGroup.interactable = enabled;
                interactionGroup.blocksRaycasts = enabled;
                interactionGroup.alpha = enabled ? 1f : disabledAlpha;
            }

            if (submitButton != null)
            {
                submitButton.interactable = enabled;
            }

            foreach (var slot in colorSlots)
            {
                slot.SetInteractable(enabled);
            }

            RefreshDreamPaletteArea();
        }

        public ColorSubmitResult TrySubmit()
        {
            var totalCount = colorSlots.Count;
            var correctCount = 0;

            foreach (var slot in colorSlots)
            {
                if (slot.IsCorrectColor())
                {
                    correctCount++;
                }
            }

            if (!dreamPaletteEnabled && forcedFailurePending)
            {
                forcedFailurePending = false;
                var failResult = new ColorSubmitResult(false, correctCount, totalCount, NextRejectLine());
                ShowBossLine(failResult.feedbackLine, ChatTaskPanelUI.Mood.Reject);

                return failResult;
            }

            var success = correctCount >= totalCount;
            var result = new ColorSubmitResult(success, correctCount, totalCount, success ? NextApproveLine() : NextRejectLine());

            if (success)
            {
                ShowBossLine(result.feedbackLine, ChatTaskPanelUI.Mood.Approve);
                QueueSuccessTransition();
            }
            else
            {
                ShowBossLine(result.feedbackLine, ChatTaskPanelUI.Mood.Reject);
            }

            return result;
        }

        private void HandleSubmitClicked()
        {
            if (!interactable)
            {
                return;
            }

            AppendPlayerSubmitLine();

            if (submitToBossReplyDelaySec > 0f && isActiveAndEnabled)
            {
                if (pendingSubmitRoutine != null)
                {
                    StopCoroutine(pendingSubmitRoutine);
                }

                pendingSubmitRoutine = StartCoroutine(DelayThenSubmit());
                return;
            }

            SubmitAfterPlayerLine();
        }

        private string NextRejectLine()
        {
            var line = rejectLines[rejectLineIndex];
            rejectLineIndex = (rejectLineIndex + 1) % rejectLines.Count;
            return line;
        }

        private string NextApproveLine()
        {
            var line = approveLines[approveLineIndex];
            approveLineIndex = (approveLineIndex + 1) % approveLines.Count;
            return line;
        }

        public void ResetPuzzle()
        {
            forcedFailurePending = forceFirstFailure;
            rejectLineIndex = 0;
            approveLineIndex = 0;
            playerSubmitIndex = 0;
            ClearCurrentPalette();
            SetDreamPaletteEnabled(false);
            SetInteractable(true);
            CancelPendingSubmit();
            CancelPendingSuccessTransition();
            successTransitionQueued = false;

            foreach (var slot in colorSlots)
            {
                slot.ResetSlot();
            }

            RefreshDreamPaletteArea();
            PresentTaskOpening();
        }

        private void HandleColorSlotClicked(ColorSlot slot)
        {
            if (!dreamPaletteEnabled || slot == null || !hasCurrentPalette)
            {
                return;
            }

            slot.ApplyPaletteColor(currentPaletteOption);
        }

        private void RefreshDreamPaletteArea()
        {
            if (dreamPaletteGroup != null)
            {
                dreamPaletteGroup.gameObject.SetActive(dreamPaletteEnabled);
                dreamPaletteGroup.alpha = dreamPaletteEnabled ? 1f : 0f;
                dreamPaletteGroup.interactable = false;
                dreamPaletteGroup.blocksRaycasts = false;
            }

            if (currentPaletteSwatch != null)
            {
                currentPaletteSwatch.Configure(0, currentPaletteOption, hasCurrentPalette, false);
                currentPaletteSwatch.SetSelected(false);
                currentPaletteSwatch.SetVisible(dreamPaletteEnabled);
            }
        }

        private void EnsureFeedbackLineFallbacks()
        {
            if (rejectLines == null)
            {
                rejectLines = new List<string>();
            }

            if (approveLines == null)
            {
                approveLines = new List<string>();
            }

            if (rejectLines.Count == 0)
            {
                rejectLines.AddRange(defaultRejectLines);
            }

            if (approveLines.Count == 0)
            {
                approveLines.AddRange(defaultApproveLines);
            }
        }

        private void BindRuntimeEvents()
        {
            foreach (var slot in colorSlots)
            {
                if (slot == null)
                {
                    continue;
                }

                slot.Clicked -= HandleColorSlotClicked;
                slot.Clicked += HandleColorSlotClicked;
            }
        }

        private void UnbindRuntimeEvents()
        {
            foreach (var slot in colorSlots)
            {
                if (slot != null)
                {
                    slot.Clicked -= HandleColorSlotClicked;
                }
            }
        }

        private void PresentTaskOpening()
        {
            ShowBossLine(initialBossLine, ChatTaskPanelUI.Mood.Neutral);
        }

        private void AutoConfigureRuntimeUi()
        {
            var uiRoot = ResolveUiRoot();
            if (uiRoot == null)
            {
                return;
            }

            if (interactionGroup == null)
            {
                interactionGroup = uiRoot.GetComponent<CanvasGroup>();
                if (interactionGroup == null)
                {
                    interactionGroup = uiRoot.gameObject.AddComponent<CanvasGroup>();
                }
            }

            EnsureFeedbackPanel(uiRoot);
            EnsureHintLabel(uiRoot);
            EnsureSubmitButton(uiRoot);
        }

        private Transform ResolveUiRoot()
        {
            var inputBox = FindDescendant(transform, "InputBox");
            if (inputBox != null)
            {
                var inputCanvas = inputBox.GetComponentInParent<Canvas>();
                if (inputCanvas != null)
                {
                    return inputCanvas.transform;
                }
            }

            for (var i = 0; i < colorSlots.Count; i++)
            {
                if (colorSlots[i] == null)
                {
                    continue;
                }

                var slotCanvas = colorSlots[i].GetComponentInParent<Canvas>();
                if (slotCanvas != null)
                {
                    return slotCanvas.transform;
                }
            }

            if (currentPaletteSwatch != null)
            {
                var swatchCanvas = currentPaletteSwatch.GetComponentInParent<Canvas>();
                if (swatchCanvas != null)
                {
                    return swatchCanvas.transform;
                }
            }

            return null;
        }

        private void EnsureFeedbackPanel(Transform uiRoot)
        {
            if (feedbackPanel != null)
            {
                return;
            }

            var inputBox = FindDescendant(transform, "InputBox");
            if (inputBox == null)
            {
                return;
            }

            var bubbleImage = inputBox.GetComponent<Image>();
            var labelTransform = FindDescendant(inputBox, RuntimeFeedbackTextName);
            Text label;

            if (labelTransform != null)
            {
                label = labelTransform.GetComponent<Text>();
            }
            else
            {
                label = CreateText(
                    RuntimeFeedbackTextName,
                    inputBox,
                    new Vector2(0f, 0f),
                    new Vector2(1f, 1f),
                    new Vector2(0f, 0f),
                    new Vector2(-48f, -44f),
                    26,
                    TextAnchor.UpperLeft);
                label.rectTransform.offsetMin = new Vector2(24f, 24f);
                label.rectTransform.offsetMax = new Vector2(-24f, -20f);
                label.supportRichText = false;
                label.horizontalOverflow = HorizontalWrapMode.Wrap;
                label.verticalOverflow = VerticalWrapMode.Truncate;
            }

            feedbackPanel = inputBox.GetComponent<BossFeedbackPanel>();
            if (feedbackPanel == null)
            {
                feedbackPanel = inputBox.gameObject.AddComponent<BossFeedbackPanel>();
            }

            feedbackPanel.Initialize(label, bubbleImage);
        }

        private void ShowBossLine(string line, ChatTaskPanelUI.Mood mood)
        {
            if (string.IsNullOrWhiteSpace(line))
            {
                return;
            }

            if (EnsureChatTaskController() != null)
            {
                chatTaskController.AppendNpcLine(line, mood);
                return;
            }

            if (feedbackPanel == null)
            {
                return;
            }

            switch (mood)
            {
                case ChatTaskPanelUI.Mood.Approve:
                    feedbackPanel.ShowApprove(line);
                    break;
                case ChatTaskPanelUI.Mood.Reject:
                    feedbackPanel.ShowReject(line);
                    break;
                default:
                    feedbackPanel.ShowNeutral(line);
                    break;
            }
        }

        private ChatTaskController EnsureChatTaskController()
        {
            if (chatTaskController == null)
            {
                chatTaskController = FindObjectOfType<ChatTaskController>(true);
            }

            return chatTaskController;
        }

        private void AppendPlayerSubmitLine()
        {
            var controller = EnsureChatTaskController();
            if (controller == null)
            {
                return;
            }

            var line = ResolvePlayerSubmitLine();
            if (string.IsNullOrWhiteSpace(line))
            {
                controller.AppendPlayerSubmit();
                return;
            }

            controller.AppendPlayerLine(line);
            playerSubmitIndex++;
        }

        private string ResolvePlayerSubmitLine()
        {
            if (playerSubmitLines == null || playerSubmitLines.Count == 0)
            {
                return string.Empty;
            }

            var index = Mathf.Clamp(playerSubmitIndex, 0, playerSubmitLines.Count - 1);
            return playerSubmitLines[index];
        }

        private IEnumerator DelayThenSubmit()
        {
            yield return new WaitForSeconds(submitToBossReplyDelaySec);
            pendingSubmitRoutine = null;
            SubmitAfterPlayerLine();
        }

        private IEnumerator DelayThenGoSuccessScene()
        {
            if (successTransitionDelaySec > 0f)
            {
                yield return new WaitForSeconds(successTransitionDelaySec);
            }

            pendingSuccessTransitionRoutine = null;

            if (!string.IsNullOrWhiteSpace(successSceneName))
            {
                SceneRouter.GoScene(successSceneName);
            }
        }

        private void SubmitAfterPlayerLine()
        {
            if (!interactable)
            {
                return;
            }

            var result = TrySubmit();
            OnSubmitAttempted?.Invoke(result);
        }

        private void QueueSuccessTransition()
        {
            if (successTransitionQueued || !isActiveAndEnabled)
            {
                return;
            }

            successTransitionQueued = true;
            CancelPendingSuccessTransition();
            pendingSuccessTransitionRoutine = StartCoroutine(DelayThenGoSuccessScene());
        }

        private void CancelPendingSubmit()
        {
            if (pendingSubmitRoutine == null)
            {
                return;
            }

            StopCoroutine(pendingSubmitRoutine);
            pendingSubmitRoutine = null;
        }

        private void CancelPendingSuccessTransition()
        {
            if (pendingSuccessTransitionRoutine == null)
            {
                return;
            }

            StopCoroutine(pendingSuccessTransitionRoutine);
            pendingSuccessTransitionRoutine = null;
        }

        private void EnsureHintLabel(Transform uiRoot)
        {
            if (hintLabel != null)
            {
                return;
            }

            var existing = FindDescendant(uiRoot, RuntimeHintName);
            if (existing != null)
            {
                hintLabel = existing.GetComponent<Text>();
                return;
            }

            hintLabel = CreateText(
                RuntimeHintName,
                uiRoot,
                new Vector2(1f, 1f),
                new Vector2(1f, 1f),
                new Vector2(-500f, -200f),
                new Vector2(400f, 96f),
                24,
                TextAnchor.UpperLeft);
            hintLabel.color = new Color(0.27f, 0.20f, 0.15f, 0.98f);
            hintLabel.horizontalOverflow = HorizontalWrapMode.Wrap;
            hintLabel.verticalOverflow = VerticalWrapMode.Overflow;
        }

        private void EnsureSubmitButton(Transform uiRoot)
        {
            if (submitButton != null)
            {
                return;
            }

            var existing = FindDescendant(uiRoot, RuntimeSubmitName);
            if (existing != null)
            {
                submitButton = existing.GetComponent<Button>();
                return;
            }

            var buttonObject = new GameObject(RuntimeSubmitName, typeof(RectTransform), typeof(Image), typeof(Button));
            var buttonTransform = buttonObject.GetComponent<RectTransform>();
            buttonTransform.SetParent(uiRoot, false);
            buttonTransform.anchorMin = new Vector2(1f, 0f);
            buttonTransform.anchorMax = new Vector2(1f, 0f);
            buttonTransform.pivot = new Vector2(1f, 0f);
            buttonTransform.anchoredPosition = new Vector2(-120f, 88f);
            buttonTransform.sizeDelta = new Vector2(220f, 74f);

            var image = buttonObject.GetComponent<Image>();
            image.color = new Color(0.93f, 0.71f, 0.25f, 0.98f);
            image.raycastTarget = true;

            var button = buttonObject.GetComponent<Button>();
            button.targetGraphic = image;
            var colors = button.colors;
            colors.normalColor = new Color(0.93f, 0.71f, 0.25f, 1f);
            colors.highlightedColor = new Color(1f, 0.80f, 0.38f, 1f);
            colors.pressedColor = new Color(0.82f, 0.58f, 0.18f, 1f);
            colors.selectedColor = colors.highlightedColor;
            colors.disabledColor = new Color(0.55f, 0.48f, 0.38f, 0.8f);
            button.colors = colors;

            var label = CreateText(
                RuntimeSubmitTextName,
                buttonTransform,
                Vector2.zero,
                Vector2.one,
                new Vector2(0f, 0f),
                Vector2.zero,
                28,
                TextAnchor.MiddleCenter);
            label.text = "提交";
            label.color = new Color(0.22f, 0.15f, 0.08f, 1f);

            submitButton = button;
        }

        private static Transform FindDescendant(Transform root, string name)
        {
            if (root == null)
            {
                return null;
            }

            if (root.name == name)
            {
                return root;
            }

            for (var i = 0; i < root.childCount; i++)
            {
                var match = FindDescendant(root.GetChild(i), name);
                if (match != null)
                {
                    return match;
                }
            }

            return null;
        }

        private static Text CreateText(
            string objectName,
            Transform parent,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Vector2 anchoredPosition,
            Vector2 sizeDelta,
            int fontSize,
            TextAnchor alignment)
        {
            var textObject = new GameObject(objectName, typeof(RectTransform), typeof(Text));
            var rectTransform = textObject.GetComponent<RectTransform>();
            rectTransform.SetParent(parent, false);
            rectTransform.anchorMin = anchorMin;
            rectTransform.anchorMax = anchorMax;
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
            rectTransform.anchoredPosition = anchoredPosition;
            rectTransform.sizeDelta = sizeDelta;

            var text = textObject.GetComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            text.fontSize = fontSize;
            text.alignment = alignment;
            text.color = new Color(0.18f, 0.14f, 0.10f, 1f);
            text.text = string.Empty;
            return text;
        }
    }

    public readonly struct ColorSubmitResult
    {
        public ColorSubmitResult(bool success, int correctCount, int totalCount, string feedbackLine)
        {
            this.success = success;
            this.correctCount = correctCount;
            this.totalCount = totalCount;
            this.feedbackLine = feedbackLine;
        }

        public bool success { get; }
        public int correctCount { get; }
        public int totalCount { get; }
        public string feedbackLine { get; }
    }
}
