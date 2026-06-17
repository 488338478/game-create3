using System;
using UnityEngine;

namespace GameCreate3
{
    public sealed class InteractionDetector : MonoBehaviour
    {
        [SerializeField] private Transform scanOrigin;
        [SerializeField] private float scanRadius = 1f;
        [SerializeField] private LayerMask interactableMask = ~0;
        [SerializeField] private KeyCode interactionKey = KeyCode.E;

        private Collider2D[] buffer = new Collider2D[16];
        private IInteractable current;

        public IInteractable Current => current;
        public event Action<IInteractable> OnCurrentInteractableChanged;

        private void Update()
        {
            RefreshCurrent();
            if (current != null && Input.GetKeyDown(interactionKey))
            {
                current.Interact(gameObject);
            }
        }

        private void RefreshCurrent()
        {
            var origin = scanOrigin != null ? scanOrigin.position : transform.position;
            var count = Physics2D.OverlapCircleNonAlloc(origin, scanRadius, buffer, interactableMask);

            IInteractable best = null;
            var bestSqrDistance = float.MaxValue;
            for (var i = 0; i < count; i++)
            {
                var candidateCollider = buffer[i];
                if (candidateCollider == null)
                {
                    continue;
                }

                var candidate = candidateCollider.GetComponentInParent<IInteractable>();
                if (candidate == null || !candidate.CanInteract(gameObject))
                {
                    continue;
                }

                var sqrDistance = (candidateCollider.transform.position - origin).sqrMagnitude;
                if (sqrDistance < bestSqrDistance)
                {
                    best = candidate;
                    bestSqrDistance = sqrDistance;
                }
            }

            if (!ReferenceEquals(current, best))
            {
                current = best;
                OnCurrentInteractableChanged?.Invoke(current);
            }
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.yellow;
            var origin = scanOrigin != null ? scanOrigin.position : transform.position;
            Gizmos.DrawWireSphere(origin, scanRadius);
        }
    }
}
