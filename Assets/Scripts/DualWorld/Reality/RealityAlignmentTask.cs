using System;
using System.Collections.Generic;
using UnityEngine;

namespace GameCreate3.DualWorld
{
    public sealed class RealityAlignmentTask : MonoBehaviour
    {
        [SerializeField] private string taskId = "alignment.right";
        [SerializeField] private CanvasGroup interactionGroup;
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

        public bool IsInteractable => interactable;

        private void Awake()
        {
            taskStartTime = Time.time;
        }

        public void SetAssistEnabled(bool enabled)
        {
            assistEnabled = enabled;

            foreach (var block in blocks)
            {
                if (block != null) block.SetAssistEnabled(enabled);
            }

            // 目标位始终可见 —— 用透明度区分严格 / 辅助两档，玩家至少知道往哪拖。
            // 之前的 SetActive 切换会让目标位完全消失，UX 上像"没反应"。
            for (var i = 0; i < targetRects.Count; i++)
            {
                if (targetRects[i] == null) continue;
                if (targetRects[i].TryGetComponent<UnityEngine.UI.Image>(out var img))
                {
                    var c = img.color;
                    img.color = new Color(c.r, c.g, c.b, enabled ? 0.7f : 0.3f);
                }
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

        /// <summary>外部（ChatBoxUI.SubmitRequested → AlignmentSubLevelFlow）调用，触发一次提交评估。</summary>
        public void Submit()
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
