using System.Collections.Generic;
using UnityEngine;

namespace GameCreate3
{
    public sealed class ExitPoint : SideScrollInteractableBase
    {
        [SerializeField] private string exitId = "default";
        [SerializeField] private bool requireGameplayCompletion;
        [SerializeField] private List<ConditionRequirementData> requirements = new List<ConditionRequirementData>();

        public override bool CanInteract(GameObject interactor)
        {
            if (!base.CanInteract(interactor) || !TryGetWorkspace(out var workspace))
            {
                return false;
            }

            if (requireGameplayCompletion && workspace is SideScrollGameplayWorkspace gameplayWorkspace && !gameplayWorkspace.IsCompleted)
            {
                return false;
            }

            return workspace.EvaluateRequirements(requirements);
        }

        public override void Interact(GameObject interactor)
        {
            if (!CanInteract(interactor) || !TryGetWorkspace(out var workspace))
            {
                return;
            }

            workspace.RaiseWorkspaceEvent($"exit.{exitId}");
            if (workspace is SideScrollGameplayWorkspace gameplayWorkspace)
            {
                gameplayWorkspace.MarkCompletedFromExit();
            }

            workspace.Exit();
        }
    }
}
