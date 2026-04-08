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

        private CanvasGroup interactionGroup;
        private Button submitButton;
        private BossFeedbackPanel feedbackPanel;
        private Text hintLabel;
        private List<DraggableLayoutBlock> blocks;
        private List<RectTransform> targetRects;
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
                interactionGroup.alpha = 1f;
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

            foreach (var block in blocks)
            {
                if (block.IsAligned(assistEnabled ? 50f : 4f))
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
