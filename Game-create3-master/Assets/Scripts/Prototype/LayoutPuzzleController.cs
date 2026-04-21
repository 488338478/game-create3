using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace GameCreate3
{
    public sealed class LayoutPuzzleController : MonoBehaviour
    {
        private readonly string[] rejectLines =
        {
            "不对，再调一下。",
            "还是不够平衡。",
            "再顺一顺结构。",
            "这版看着还是别扭。"
        };

        [SerializeField] private CanvasGroup interactionGroup;
        [SerializeField] private Button submitButton;
        [SerializeField] private BossFeedbackPanel feedbackPanel;
        [SerializeField] private Text hintLabel;
        [SerializeField] private List<DraggableLayoutBlock> blocks = new List<DraggableLayoutBlock>();
        [SerializeField] private List<RectTransform> targetRects = new List<RectTransform>();
        [SerializeField] private bool forceFirstFailure = true;
        [SerializeField] private float strictTolerance = 4f;
        [SerializeField] private float assistedTolerance = 50f;
        [SerializeField] private float disabledAlpha = 0.42f;

        private bool assistEnabled;
        private bool interactable;
        private bool forcedFailurePending = true;
        private int rejectLineIndex;

        public event Action<LayoutSubmitResult> OnSubmitAttempted;

        public void Initialize(
            CanvasGroup canvasGroup,
            Button submit,
            BossFeedbackPanel bossFeedback,
            Text hint,
            List<DraggableLayoutBlock> blockList,
            List<RectTransform> targets)
        {
            interactionGroup = canvasGroup;
            submitButton = submit;
            feedbackPanel = bossFeedback;
            hintLabel = hint;
            blocks = blockList ?? new List<DraggableLayoutBlock>();
            targetRects = targets ?? new List<RectTransform>();

            foreach (var block in blocks)
            {
                block.SetAssistEnabled(false);
                block.SetInteractable(true);
            }

            for (var i = 0; i < targetRects.Count; i++)
            {
                targetRects[i].gameObject.SetActive(false);
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
            List<DraggableLayoutBlock> blockList,
            List<RectTransform> targets)
        {
            Initialize(canvasGroup, submit, bossFeedback, hint, blockList, targets);
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

            SetAssistEnabled(false);
            SetInteractable(true);
        }

        private void OnDisable()
        {
            if (submitButton != null)
            {
                submitButton.onClick.RemoveListener(HandleSubmitClicked);
            }
        }

        public void SetAssistEnabled(bool enabled)
        {
            assistEnabled = enabled;

            foreach (var block in blocks)
            {
                block.SetAssistEnabled(enabled);
            }

            for (var i = 0; i < targetRects.Count; i++)
            {
                targetRects[i].gameObject.SetActive(enabled);
            }

            if (hintLabel != null)
            {
                hintLabel.text = enabled
                    ? "梦境回流：目标位出现高亮，模块会自动吸附。"
                    : "先拖动右侧模块，第一次提交一定会被打回。";
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

            foreach (var block in blocks)
            {
                block.SetInteractable(enabled);
            }
        }

        public LayoutSubmitResult TrySubmit()
        {
            var totalCount = blocks.Count;
            var alignedCount = 0;
            var tolerance = assistEnabled ? assistedTolerance : strictTolerance;

            foreach (var block in blocks)
            {
                if (block.IsAligned(tolerance))
                {
                    alignedCount++;
                }
            }

            if (!assistEnabled && forcedFailurePending)
            {
                forcedFailurePending = false;
                var failResult = new LayoutSubmitResult(false, alignedCount, totalCount, NextRejectLine());
                if (feedbackPanel != null)
                {
                    feedbackPanel.ShowReject(failResult.feedbackLine);
                }

                return failResult;
            }

            var success = alignedCount >= totalCount;
            var result = new LayoutSubmitResult(success, alignedCount, totalCount, success ? "这次可以了。" : NextRejectLine());

            if (feedbackPanel != null)
            {
                if (success)
                {
                    feedbackPanel.ShowApprove(result.feedbackLine);
                }
                else
                {
                    feedbackPanel.ShowReject(result.feedbackLine + "\n" + alignedCount + " / " + totalCount + " 模块已就位");
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
            SetAssistEnabled(false);
            SetInteractable(true);
            foreach (var block in blocks)
            {
                block.ResetBlock();
            }
        }
    }

    public readonly struct LayoutSubmitResult
    {
        public LayoutSubmitResult(bool success, int alignedCount, int totalCount, string feedbackLine)
        {
            this.success = success;
            this.alignedCount = alignedCount;
            this.totalCount = totalCount;
            this.feedbackLine = feedbackLine;
        }

        public bool success { get; }
        public int alignedCount { get; }
        public int totalCount { get; }
        public string feedbackLine { get; }
    }
}
