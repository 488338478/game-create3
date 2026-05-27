using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace GameCreate3.DualWorld
{
    public sealed class DraggableAlignmentBlock : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        [Tooltip("候选 target 数组：松手时，挑数组中 active 且未被其他 block 占用、且最近的吸过去。\n通常由 RealityAlignmentTask 在 Awake 注入 task.targetRects；Inspector 配的值仅作编辑期预览，运行时会被覆盖。")]
        [SerializeField] private List<RectTransform> targets = new List<RectTransform>();
        [SerializeField] private float assistedSnapRange = 60f;

        // 全场 block 共享，防止两个 block 同时吸同一个 target。
        private static readonly HashSet<RectTransform> claimedTargets = new HashSet<RectTransform>();

        private RectTransform rectTransform;
        private Canvas canvas;
        private RectTransform boundsRect;
        private bool assistEnabled;
        private bool interactable = true;
        private bool snapped;
        private Vector2 initialPosition;
        private RectTransform snappedTarget;

        public IReadOnlyList<RectTransform> Targets => targets;
        public RectTransform SnappedTarget => snappedTarget;
        public bool IsSnapped => snapped;

        public event Action<DraggableAlignmentBlock> Snapped;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetSharedClaims()
        {
            // Disable Domain Reload 模式下 static 容器会跨场景残留，运行入口清一遍。
            claimedTargets.Clear();
        }

        private void Awake()
        {
            rectTransform = (RectTransform)transform;
            canvas = GetComponentInParent<Canvas>();
            initialPosition = rectTransform.anchoredPosition;
        }

        public void SetTargets(IList<RectTransform> candidates)
        {
            // 防御：task.targetRects 未配置（空 / 全 null）时不要清掉 block 自己 Inspector 配的 targets。
            // 历史踩坑：RealityAlignmentTask.AutoWireBlocksAndTargets 调这里时，如果 task 没配 targetRects，
            // 之前的实现会先 targets.Clear() 把 block 自带的也抹掉，导致 block 运行时无 target 可吸附。
            if (candidates == null || candidates.Count == 0) return;

            var hasAny = false;
            for (var i = 0; i < candidates.Count; i++)
            {
                if (candidates[i] != null) { hasAny = true; break; }
            }
            if (!hasAny) return;

            targets.Clear();
            for (var i = 0; i < candidates.Count; i++)
            {
                if (candidates[i] != null) targets.Add(candidates[i]);
            }
        }

        public void SetBounds(RectTransform bounds)
        {
            boundsRect = bounds;
        }

        public void SetSnapRange(float range)
        {
            if (range > 0f) assistedSnapRange = range;
        }

        public void SetAssistEnabled(bool value)
        {
            assistEnabled = value;
        }

        public void SetInteractable(bool value)
        {
            interactable = value;
        }

        public void ResetBlock()
        {
            rectTransform.anchoredPosition = initialPosition;
            if (snappedTarget != null) claimedTargets.Remove(snappedTarget);
            snappedTarget = null;
            snapped = false;
            interactable = true;
        }

        public bool IsAligned(float tolerance)
        {
            var t = snappedTarget;
            if (t == null) return false;
            var diff = (Vector2)(t.position - rectTransform.position);
            return diff.magnitude <= tolerance;
        }

        public void OnBeginDrag(PointerEventData eventData) { }

        public void OnDrag(PointerEventData eventData)
        {
            if (!interactable || canvas == null) return;
            rectTransform.anchoredPosition += eventData.delta / canvas.scaleFactor;
            ClampInsideBounds();
            // 不在拖拽中触发吸附 —— 玩家松手那一下才检测。
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            ClampInsideBounds();
            TrySnapToNearestActiveTarget();
        }

        private void TrySnapToNearestActiveTarget()
        {
            if (snapped || !interactable) return;

            var range = assistedSnapRange;
            var bestSqr = range * range;
            RectTransform best = null;

            for (var i = 0; i < targets.Count; i++)
            {
                var t = targets[i];
                if (!IsSnapCandidate(t)) continue;
                var dSqr = ((Vector2)(t.position - rectTransform.position)).sqrMagnitude;
                if (dSqr <= bestSqr)
                {
                    bestSqr = dSqr;
                    best = t;
                }
            }

            if (best == null) return;

            rectTransform.position = best.position;
            snappedTarget = best;
            claimedTargets.Add(best);
            snapped = true;
            interactable = false;
            Snapped?.Invoke(this);
        }

        private bool IsSnapCandidate(RectTransform t)
        {
            if (t == null) return false;
            if (!t.gameObject.activeInHierarchy) return false;
            if (claimedTargets.Contains(t)) return false;
            return true;
        }

        private void ClampInsideBounds()
        {
            if (boundsRect == null || rectTransform == null)
            {
                return;
            }

            var parent = rectTransform.parent as RectTransform;
            if (parent == null)
            {
                return;
            }

            var size = rectTransform.rect.size;
            var pivot = rectTransform.pivot;
            var scale = rectTransform.lossyScale;
            var parentScale = parent.lossyScale;
            var sx = parentScale.x == 0f ? 1f : scale.x / parentScale.x;
            var sy = parentScale.y == 0f ? 1f : scale.y / parentScale.y;
            var scaledSize = new Vector2(size.x * sx, size.y * sy);

            var worldCorners = new Vector3[4];
            boundsRect.GetWorldCorners(worldCorners);
            var bottomLeft = (Vector2)parent.InverseTransformPoint(worldCorners[0]);
            var topRight = (Vector2)parent.InverseTransformPoint(worldCorners[2]);

            var minX = bottomLeft.x + scaledSize.x * pivot.x;
            var maxX = topRight.x - scaledSize.x * (1f - pivot.x);
            var minY = bottomLeft.y + scaledSize.y * pivot.y;
            var maxY = topRight.y - scaledSize.y * (1f - pivot.y);

            if (minX > maxX)
            {
                minX = maxX = (minX + maxX) * 0.5f;
            }
            if (minY > maxY)
            {
                minY = maxY = (minY + maxY) * 0.5f;
            }

            var pos = rectTransform.anchoredPosition;
            pos.x = Mathf.Clamp(pos.x, minX, maxX);
            pos.y = Mathf.Clamp(pos.y, minY, maxY);
            rectTransform.anchoredPosition = pos;
        }
    }
}
