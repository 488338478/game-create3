using System;
using System.Collections.Generic;
using UnityEngine;

namespace GameCreate3.DualWorld
{
    public sealed class RealityAlignmentTask : MonoBehaviour
    {
        [Serializable]
        public struct ObservationUnlock
        {
            public string observationId;   // 对应背景中 ObservationPoint 的 id（如 book）
            public int blockIndex;         // 对应 blocks/targetRects 索引
        }

        [SerializeField] private string taskId = "alignment.right";
        [SerializeField] private CanvasGroup interactionGroup;
        [SerializeField] private List<DraggableAlignmentBlock> blocks = new List<DraggableAlignmentBlock>();
        [SerializeField] private List<RectTransform> targetRects = new List<RectTransform>();
        [SerializeField] private RectTransform dragBounds;
        [SerializeField] private float strictTolerance = 4f;
        [SerializeField] private float assistedTolerance = 50f;
        [SerializeField] private float disabledAlpha = 0.42f;

        [Header("Per-target unlock")]
        [SerializeField] private List<ObservationUnlock> unlockMap = new List<ObservationUnlock>();
        [SerializeField] private float snapRange = 60f;

        private bool assistEnabled;
        private bool interactable;
        private int failCount;
        private float taskStartTime;

        private readonly HashSet<int> snappedBlockIndices = new HashSet<int>();
        private readonly HashSet<string> consumedObservationIds = new HashSet<string>();

        private SideScrollWorkspaceBase workspace;
        private bool workspaceSubscribed;

        public event Action<RealitySubmitResult> SubmitAttempted;

        public bool IsInteractable => interactable;
        public int SnappedCount => snappedBlockIndices.Count;
        public int TotalBlocks => blocks.Count;

        private void Awake()
        {
            taskStartTime = Time.time;
            AutoWireBlocksAndTargets();
            ApplyBoundsToBlocks();
            ApplySnapRangeToBlocks();
            HookBlockSnapEvents();
            HideAllTargets();
        }

        private void OnEnable()
        {
            EnsureWorkspaceSubscription();
        }

        private void OnDisable()
        {
            UnsubscribeWorkspace();
        }

        private void OnDestroy()
        {
            UnhookBlockSnapEvents();
            UnsubscribeWorkspace();
        }

        private void AutoWireBlocksAndTargets()
        {
            if (blocks.Count == 0)
            {
                blocks.AddRange(GetComponentsInChildren<DraggableAlignmentBlock>(true));
            }

            targetRects.Clear();
            foreach (var block in blocks)
            {
                targetRects.Add(block != null ? block.Target : null);
            }
        }

        private void ApplyBoundsToBlocks()
        {
            if (dragBounds == null) return;
            foreach (var block in blocks)
            {
                if (block != null) block.SetBounds(dragBounds);
            }
        }

        private void ApplySnapRangeToBlocks()
        {
            if (snapRange <= 0f) return;
            foreach (var block in blocks)
            {
                if (block != null) block.SetSnapRange(snapRange);
            }
        }

        private void HookBlockSnapEvents()
        {
            foreach (var block in blocks)
            {
                if (block == null) continue;
                block.Snapped -= OnBlockSnapped;
                block.Snapped += OnBlockSnapped;
            }
        }

        private void UnhookBlockSnapEvents()
        {
            foreach (var block in blocks)
            {
                if (block == null) continue;
                block.Snapped -= OnBlockSnapped;
            }
        }

        private void HideAllTargets()
        {
            for (var i = 0; i < targetRects.Count; i++)
            {
                var t = targetRects[i];
                if (t == null) continue;
                t.gameObject.SetActive(false);
            }
        }

        private void EnsureWorkspaceSubscription()
        {
            if (workspaceSubscribed) return;
            if (workspace == null) workspace = GetComponentInParent<SideScrollWorkspaceBase>();
            if (workspace == null) return;
            workspace.WorkspaceEventRaised += OnWorkspaceEvent;
            workspaceSubscribed = true;
        }

        private void UnsubscribeWorkspace()
        {
            if (!workspaceSubscribed || workspace == null) return;
            workspace.WorkspaceEventRaised -= OnWorkspaceEvent;
            workspaceSubscribed = false;
        }

        private void OnWorkspaceEvent(string eventId)
        {
            if (string.IsNullOrEmpty(eventId)) return;
            const string prefix = "observation.";
            if (!eventId.StartsWith(prefix, StringComparison.Ordinal)) return;

            var observationId = eventId.Substring(prefix.Length);
            if (!consumedObservationIds.Add(observationId)) return;

            for (var i = 0; i < unlockMap.Count; i++)
            {
                if (!string.Equals(unlockMap[i].observationId, observationId, StringComparison.Ordinal)) continue;
                UnlockTarget(unlockMap[i].blockIndex);
            }
        }

        private void UnlockTarget(int blockIndex)
        {
            if (blockIndex < 0 || blockIndex >= targetRects.Count) return;
            var rect = targetRects[blockIndex];
            if (rect == null) return;

            if (rect.TryGetComponent<AlignmentTargetUnlocker>(out var unlocker))
            {
                unlocker.Play();
            }
            else
            {
                rect.gameObject.SetActive(true);
            }
        }

        public void UnlockAllRemainingTargets()
        {
            for (var i = 0; i < targetRects.Count; i++)
            {
                if (targetRects[i] == null) continue;
                if (targetRects[i].gameObject.activeSelf) continue;
                UnlockTarget(i);
            }
        }

        public void SetAssistEnabled(bool enabled)
        {
            assistEnabled = enabled;

            foreach (var block in blocks)
            {
                if (block != null) block.SetAssistEnabled(enabled);
            }

            // 视觉反馈：辅助模式下把已激活的 target 调亮一点；未激活的 target 还是隐藏，不动它们。
            for (var i = 0; i < targetRects.Count; i++)
            {
                if (targetRects[i] == null) continue;
                if (!targetRects[i].gameObject.activeSelf) continue;
                if (targetRects[i].TryGetComponent<UnityEngine.UI.Image>(out var img))
                {
                    var c = img.color;
                    img.color = new Color(c.r, c.g, c.b, enabled ? 0.95f : 0.7f);
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
                if (block == null) continue;
                // 已经吸附锁定的 block 不会因 SetInteractable(true) 解锁。
                if (block.IsSnapped) continue;
                block.SetInteractable(enabled);
            }
        }

        public void ResetTask()
        {
            failCount = 0;
            taskStartTime = Time.time;
            snappedBlockIndices.Clear();
            consumedObservationIds.Clear();
            SetAssistEnabled(false);
            HideAllTargets();
            foreach (var block in blocks)
            {
                if (block != null) block.ResetBlock();
            }
            SetInteractable(true);
        }

        /// <summary>外部（ChatBoxUI.SubmitRequested → AlignmentSubLevelFlow）调用，触发一次提交评估。</summary>
        public void Submit()
        {
            if (!interactable) return;

            var lockedCount = snappedBlockIndices.Count;
            var totalCount = blocks.Count;
            var success = totalCount > 0 && lockedCount >= totalCount;
            string failReason = null;

            if (!success)
            {
                failCount++;
                var missing = Mathf.Max(0, totalCount - lockedCount);
                failReason = missing > 0
                    ? $"还差 {missing} 块没归位"
                    : "结构不对，先去梦境理顺空间感";
            }

            var result = new RealitySubmitResult(taskId, success, failReason, failCount, Time.time - taskStartTime);
            SubmitAttempted?.Invoke(result);
        }

        private void OnBlockSnapped(DraggableAlignmentBlock block)
        {
            if (block == null) return;
            var idx = blocks.IndexOf(block);
            if (idx < 0) return;
            if (!snappedBlockIndices.Add(idx)) return;

            EnsureWorkspaceSubscription();

            var snapEventId = $"alignment.snap.{taskId}.{idx}";
            workspace?.RegisterGoal(snapEventId);
            workspace?.RaiseWorkspaceEvent(snapEventId);
        }
    }
}
