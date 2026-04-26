using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace GameCreate3
{
    public sealed class ColorPuzzleController : MonoBehaviour
    {
        private readonly string[] rejectLines =
        {
            "配色不对，再调整一下。",
            "颜色搭配不够和谐。",
            "这个配色不太符合要求。",
            "再试试看其他颜色组合。"
        };

        [SerializeField] private CanvasGroup interactionGroup;
        [SerializeField] private Button submitButton;
        [SerializeField] private BossFeedbackPanel feedbackPanel;
        [SerializeField] private Text hintLabel;
        [SerializeField] private List<ColorSlot> colorSlots = new List<ColorSlot>();
        [SerializeField] private bool forceFirstFailure = true;
        [SerializeField] private float disabledAlpha = 0.42f;

        private bool dreamPaletteEnabled;
        private bool interactable;
        private bool forcedFailurePending = true;
        private int rejectLineIndex;
        private List<Color> dreamPaletteColors = new List<Color>();

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
            forcedFailurePending = forceFirstFailure;
        }

        private void OnEnable()
        {
            if (submitButton != null)
            {
                submitButton.onClick.RemoveListener(HandleSubmitClicked);
                submitButton.onClick.AddListener(HandleSubmitClicked);
            }

            SetDreamPaletteEnabled(false);
            SetInteractable(true);
        }

        private void OnDisable()
        {
            if (submitButton != null)
            {
                submitButton.onClick.RemoveListener(HandleSubmitClicked);
            }
        }

        public void SetDreamPaletteEnabled(bool enabled)
        {
            dreamPaletteEnabled = enabled;

            foreach (var slot in colorSlots)
            {
                slot.SetDreamPaletteEnabled(enabled);
            }

            if (hintLabel != null)
            {
                hintLabel.text = enabled
                    ? "梦境色卡已解锁：使用左侧收集的颜色完成配色。"
                    : "先尝试配色，第一次提交一定会被打回。";
            }
        }

        public void SetDreamPaletteColors(IReadOnlyList<Color> colors)
        {
            dreamPaletteColors.Clear();
            if (colors != null)
            {
                dreamPaletteColors.AddRange(colors);
            }

            foreach (var slot in colorSlots)
            {
                slot.SetAvailableColors(dreamPaletteColors);
            }
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
                if (feedbackPanel != null)
                {
                    feedbackPanel.ShowReject(failResult.feedbackLine);
                }

                return failResult;
            }

            var success = correctCount >= totalCount;
            var result = new ColorSubmitResult(success, correctCount, totalCount, success ? "配色完美！" : NextRejectLine());

            if (feedbackPanel != null)
            {
                if (success)
                {
                    feedbackPanel.ShowApprove(result.feedbackLine);
                }
                else
                {
                    feedbackPanel.ShowReject(result.feedbackLine + "\n" + correctCount + " / " + totalCount + " 颜色正确");
                }
            }

            return result;
        }

        private void HandleSubmitClicked()
        {
            if (!interactable)
            {
                return;
            }

            var result = TrySubmit();
            OnSubmitAttempted?.Invoke(result);
        }

        private string NextRejectLine()
        {
            var line = rejectLines[rejectLineIndex];
            rejectLineIndex = (rejectLineIndex + 1) % rejectLines.Length;
            return line;
        }

        public void ResetPuzzle()
        {
            forcedFailurePending = forceFirstFailure;
            rejectLineIndex = 0;
            SetDreamPaletteEnabled(false);
            dreamPaletteColors.Clear();
            SetInteractable(true);
            foreach (var slot in colorSlots)
            {
                slot.ResetSlot();
            }
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
