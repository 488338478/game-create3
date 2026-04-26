using UnityEngine;

namespace GameCreate3
{
    public sealed class SideScrollDebugInteractable : SideScrollInteractableBase
    {
        [SerializeField] private string eventId = "debug.interactable";

        public override void Interact(GameObject interactor)
        {
            if (TryGetWorkspace(out var workspace))
            {
                workspace.RaiseWorkspaceEvent(eventId);
            }

            Debug.Log($"[SideScroll] Interacted with {name}.");
        }
    }
}
