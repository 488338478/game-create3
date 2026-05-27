using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace GameCreate3.DualWorld
{
    public sealed class DraggableAlignmentBlock : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        [SerializeField] private RectTransform target;
        [SerializeField] private float assistedSnapRange = 60f;

        private RectTransform rectTransform;
        private Canvas canvas;
        private RectTransform boundsRect;
        private bool assistEnabled;
        private bool interactable = true;
        private bool snapped;
        private Vector2 initialPosition;

        public RectTransform Target => target;
        public bool IsSnapped => snapped;

        public event Action<DraggableAlignmentBlock> Snapped;

        private void Awake()
        {
            rectTransform = (RectTransform)transform;
            canvas = GetComponentInParent<Canvas>();
            initialPosition = rectTransform.anchoredPosition;
        }

        public void SetTarget(RectTransform t)
        {
            target = t;
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
            snapped = false;
            interactable = true;
        }

        public bool IsAligned(float tolerance)
        {
            if (target == null)
            {
                return false;
            }

            var diff = (Vector2)(target.position - rectTransform.position);
            return diff.magnitude <= tolerance;
        }

        public void OnBeginDrag(PointerEventData eventData) { }

        public void OnDrag(PointerEventData eventData)
        {
            if (!interactable || canvas == null)
            {
                return;
            }

            rectTransform.anchoredPosition += eventData.delta / canvas.scaleFactor;
            ClampInsideBounds();
            TrySnapToActiveTarget();
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            ClampInsideBounds();
            TrySnapToActiveTarget();
        }

        private void TrySnapToActiveTarget()
        {
            if (snapped || !interactable || target == null) return;
            if (!target.gameObject.activeInHierarchy) return;

            var diff = (Vector2)(target.position - rectTransform.position);
            if (diff.magnitude > assistedSnapRange) return;

            rectTransform.position = target.position;
            snapped = true;
            interactable = false;
            Snapped?.Invoke(this);
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
