using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace GameCreate3.DualWorld
{
    public sealed class RealityAlignmentTask : MonoBehaviour
    {
        [SerializeField] private string taskId = "alignment.right";
        [SerializeField] private CanvasGroup interactionGroup;
        [SerializeField] private Button submitButton;
        [SerializeField] private List<DraggableAlignmentBlock> blocks = new List<DraggableAlignmentBlock>();
        [SerializeField] private List<RectTransform> targetRects = new List<RectTransform>();
        [SerializeField] private float strictTolerance = 4f;
        [SerializeField] private float assistedTolerance = 50f;
        [SerializeField] private float disabledAlpha = 0.42f;

        private bool assistEnabled;
        private bool interactable;
        private int failCount;
        private float taskStartTime;

        public event Action<RealitySubmitResult> SubmitAttempted;

        private void Awake()
        {
            if (submitButton != null)
            {
                submitButton.onClick.AddListener(HandleSubmitClicked);
            }

            taskStartTime = Time.time;
        }

        private void OnDestroy()
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
                if (block != null) block.SetAssistEnabled(enabled);
            }

            for (var i = 0; i < targetRects.Count; i++)
            {
                if (targetRects[i] != null) targetRects[i].gameObject.SetActive(enabled);
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
                if (block != null) block.SetInteractable(enabled);
            }
        }

        public void ResetTask()
        {
            failCount = 0;
            taskStartTime = Time.time;
            SetAssistEnabled(false);
            SetInteractable(true);
            foreach (var block in blocks)
            {
                if (block != null) block.ResetBlock();
            }
        }

        private void HandleSubmitClicked()
        {
            if (!interactable) return;

            var tolerance = assistEnabled ? assistedTolerance : strictTolerance;
            var alignedCount = 0;
            foreach (var block in blocks)
            {
                if (block != null && block.IsAligned(tolerance)) alignedCount++;
            }

            var success = assistEnabled && alignedCount >= blocks.Count;
            string failReason = null;

            if (!success)
            {
                failCount++;
                failReason = assistEnabled
                    ? $"还差一点：{alignedCount}/{blocks.Count}"
                    : "结构不对，先去梦境理顺空间感";
            }

            var result = new RealitySubmitResult(taskId, success, failReason, failCount, Time.time - taskStartTime);
            SubmitAttempted?.Invoke(result);
        }
    }
}
