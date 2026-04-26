using UnityEngine;

namespace GameCreate3
{
    public sealed class WorkspaceEventTriggerZone : TriggerZoneBase
    {
        [SerializeField] private string eventId = "trigger.default";
        [SerializeField] private bool raiseOnExit;

        protected override void OnTriggered(Collider2D other)
        {
            Workspace?.RaiseWorkspaceEvent(eventId);
        }

        protected override void OnUntriggered(Collider2D other)
        {
            if (raiseOnExit)
            {
                Workspace?.RaiseWorkspaceEvent($"{eventId}.exit");
            }
        }
    }
}
