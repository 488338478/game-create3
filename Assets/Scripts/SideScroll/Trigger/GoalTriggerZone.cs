using UnityEngine;

namespace GameCreate3
{
    public sealed class GoalTriggerZone : TriggerZoneBase
    {
        [SerializeField] private string goalId = "default";

        protected override void OnTriggered(Collider2D other)
        {
            Workspace?.RegisterGoal(goalId);
            Workspace?.RaiseWorkspaceEvent($"goal.{goalId}");

            if (other.TryGetComponent<PushableObject>(out var pushable))
            {
                pushable.ReportSolved();
            }
        }
    }
}
