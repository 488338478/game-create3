using UnityEngine;

namespace GameCreate3
{
    [RequireComponent(typeof(Collider2D))]
    public abstract class TriggerZoneBase : MonoBehaviour
    {
        [SerializeField] private LayerMask targetLayers = ~0;
        [SerializeField] private bool oneShot;
        [SerializeField] private float cooldown;

        protected SideScrollWorkspaceBase Workspace { get; private set; }

        private bool hasTriggered;
        private float cooldownUntil;

        public void BindWorkspace(SideScrollWorkspaceBase workspace)
        {
            Workspace = workspace;
            if (TryGetComponent<Collider2D>(out var collider2D))
            {
                collider2D.isTrigger = true;
            }
        }

        protected virtual bool CanTrigger(Collider2D other)
        {
            if (Workspace == null || (oneShot && hasTriggered) || Time.time < cooldownUntil)
            {
                return false;
            }

            return ((1 << other.gameObject.layer) & targetLayers.value) != 0;
        }

        protected virtual void OnTriggered(Collider2D other)
        {
        }

        protected virtual void OnUntriggered(Collider2D other)
        {
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!CanTrigger(other))
            {
                return;
            }

            hasTriggered = true;
            cooldownUntil = Time.time + cooldown;
            OnTriggered(other);
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            if (Workspace == null)
            {
                return;
            }

            OnUntriggered(other);
        }
    }
}
