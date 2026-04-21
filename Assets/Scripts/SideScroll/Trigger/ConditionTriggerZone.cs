using System.Collections.Generic;
using UnityEngine;

namespace GameCreate3
{
    public sealed class ConditionTriggerZone : TriggerZoneBase
    {
        [SerializeField] private string conditionId = "default";
        [SerializeField] private List<ConditionRequirementData> requirements = new List<ConditionRequirementData>();

        protected override void OnTriggered(Collider2D other)
        {
            if (Workspace != null && Workspace.EvaluateRequirements(requirements))
            {
                Workspace.RaiseWorkspaceEvent($"condition.{conditionId}.passed");
            }
        }
    }
}
