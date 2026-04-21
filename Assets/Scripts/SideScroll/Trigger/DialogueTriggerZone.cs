using UnityEngine;

namespace GameCreate3
{
    public sealed class DialogueTriggerZone : TriggerZoneBase
    {
        [SerializeField] private string dialogueId = "default";

        protected override void OnTriggered(Collider2D other)
        {
            Workspace?.RaiseWorkspaceEvent($"dialogue.{dialogueId}");
        }
    }
}
