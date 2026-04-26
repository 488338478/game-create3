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
        private bool assistEnabled;
        private bool interactable = true;
        private Vector2 initialPosition;

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
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (!assistEnabled || target == null)
            {
                return;
            }

            var diff = (Vector2)(target.position - rectTransform.position);
            if (diff.magnitude <= assistedSnapRange)
            {
                rectTransform.position = target.position;
            }
        }
    }
}
