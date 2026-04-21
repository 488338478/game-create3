using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace GameCreate3
{
    public sealed class DraggableLayoutBlock : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        [SerializeField] private RectTransform rectTransform;
        [SerializeField] private RectTransform targetRect;
        private Image image;
        private bool assistEnabled;
        private bool interactable = true;
        private bool isDragging;
        private bool locked;
        private Vector2 pointerOffset;
        private Vector2 initialPosition;

        public void Initialize(RectTransform blockRect, RectTransform target)
        {
            rectTransform = blockRect;
            targetRect = target;
            CacheComponents();
        }

        private void Awake()
        {
            if (rectTransform == null)
            {
                rectTransform = transform as RectTransform;
            }

            CacheComponents();
            if (rectTransform != null)
            {
                initialPosition = rectTransform.anchoredPosition;
            }
        }

        public void SetAssistEnabled(bool enabled)
        {
            assistEnabled = enabled;
            locked = false;
            UpdateVisual();
        }

        public void SetInteractable(bool enabled)
        {
            interactable = enabled;
            UpdateVisual();
        }

        public bool IsAligned(float tolerance)
        {
            if (rectTransform == null || targetRect == null)
            {
                return false;
            }

            return Vector2.Distance(rectTransform.anchoredPosition, targetRect.anchoredPosition) <= tolerance;
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (!CanDrag())
            {
                return;
            }

            isDragging = true;
            locked = false;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                rectTransform.parent as RectTransform,
                eventData.position,
                eventData.pressEventCamera,
                out var localPoint);

            pointerOffset = rectTransform.anchoredPosition - localPoint;
            UpdateVisual();
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (!isDragging || rectTransform == null)
            {
                return;
            }

            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                rectTransform.parent as RectTransform,
                eventData.position,
                eventData.pressEventCamera,
                out var localPoint);

            rectTransform.anchoredPosition = localPoint + pointerOffset;
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (!isDragging)
            {
                return;
            }

            isDragging = false;

            if (assistEnabled && targetRect != null)
            {
                var distance = Vector2.Distance(rectTransform.anchoredPosition, targetRect.anchoredPosition);
                if (distance <= 110f)
                {
                    rectTransform.anchoredPosition = targetRect.anchoredPosition;
                    locked = true;
                }
            }

            UpdateVisual();
        }

        private bool CanDrag()
        {
            return interactable && !locked && rectTransform != null && targetRect != null;
        }

        private void UpdateVisual()
        {
            if (image == null)
            {
                return;
            }

            var color = image.color;
            color.a = interactable ? 1f : 0.42f;

            if (locked)
            {
                color = Color.Lerp(color, Color.white, 0.28f);
            }
            else if (assistEnabled)
            {
                color = Color.Lerp(color, Color.white, 0.1f);
            }

            image.color = color;
        }

        private void CacheComponents()
        {
            if (image == null)
            {
                image = GetComponent<Image>();
            }
        }

        public void ResetBlock()
        {
            locked = false;
            isDragging = false;
            if (rectTransform != null)
            {
                rectTransform.anchoredPosition = initialPosition;
            }
            UpdateVisual();
        }
    }
}
