using UnityEngine;

namespace GameCreate3
{
    public sealed class PickupObject : SideScrollInteractableBase
    {
        [SerializeField] private string pickupId = "default";
        [SerializeField] private bool destroyOnPickup;

        public override void Interact(GameObject interactor)
        {
            if (!TryGetWorkspace(out var workspace) || !CanInteract(interactor))
            {
                return;
            }

            workspace.RegisterPickup(pickupId);
            workspace.RaiseWorkspaceEvent($"pickup.{pickupId}");
            if (destroyOnPickup)
            {
                Destroy(gameObject);
            }
            else
            {
                gameObject.SetActive(false);
            }
        }
    }
}
