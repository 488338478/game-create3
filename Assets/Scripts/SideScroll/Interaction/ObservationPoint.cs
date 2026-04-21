using UnityEngine;

namespace GameCreate3
{
    public sealed class ObservationPoint : SideScrollInteractableBase
    {
        [SerializeField] private string observationId = "default";
        [SerializeField] private bool oneShot = true;

        private bool consumed;

        public override bool CanInteract(GameObject interactor)
        {
            return base.CanInteract(interactor) && (!oneShot || !consumed);
        }

        public override void Interact(GameObject interactor)
        {
            if (!TryGetWorkspace(out var workspace) || !CanInteract(interactor))
            {
                return;
            }

            consumed = true;
            workspace.RaiseWorkspaceEvent($"observation.{observationId}");
        }
    }
}
