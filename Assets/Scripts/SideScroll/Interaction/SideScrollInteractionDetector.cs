using UnityEngine;

namespace GameCreate3
{
    public sealed class SideScrollInteractionDetector : MonoBehaviour
    {
        [SerializeField] private Transform scanOrigin;
        [SerializeField] private float scanRadius = 0.8f;
        [SerializeField] private LayerMask interactableMask = ~0;

        private readonly Collider2D[] results = new Collider2D[8];

        public void SetInteractableMask(LayerMask mask)
        {
            interactableMask = mask;
        }

        public void ProcessInteraction(GameObject interactor, bool interactPressed)
        {
            if (!interactPressed)
            {
                return;
            }

            var origin = scanOrigin != null ? (Vector2)scanOrigin.position : (Vector2)transform.position;
            var count = Physics2D.OverlapCircleNonAlloc(origin, scanRadius, results, interactableMask);
            for (var i = 0; i < count; i++)
            {
                var candidate = results[i];
                if (candidate == null || !candidate.TryGetComponent<ISideScrollInteractable>(out var interactable))
                {
                    continue;
                }

                if (!interactable.CanInteract(interactor))
                {
                    continue;
                }

                interactable.Interact(interactor);
                break;
            }
        }

        private void OnDrawGizmosSelected()
        {
            var origin = scanOrigin != null ? scanOrigin.position : transform.position;
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(origin, scanRadius);
        }
    }
}
