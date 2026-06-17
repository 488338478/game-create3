using System;
using System.Collections.Generic;
using UnityEngine;

namespace GameCreate3
{
    public sealed class ObjectiveTracker : MonoBehaviour
    {
        [SerializeField] private NarrativeVariableStore variableStore;
        [SerializeField] private List<ObjectiveDefinition> objectives = new List<ObjectiveDefinition>();
        [SerializeField] private List<string> completedObjectiveIds = new List<string>();

        public event Action<ObjectiveDefinition> OnCurrentObjectiveChanged;

        public IReadOnlyList<string> CompletedObjectiveIds => completedObjectiveIds;
        public ObjectiveDefinition CurrentObjective { get; private set; }

        private void OnEnable()
        {
            if (variableStore != null)
            {
                variableStore.OnVariableChanged += HandleVariableChanged;
            }

            Refresh();
        }

        private void OnDisable()
        {
            if (variableStore != null)
            {
                variableStore.OnVariableChanged -= HandleVariableChanged;
            }
        }

        public void RestoreCompletedObjectives(IReadOnlyList<string> completedIds)
        {
            completedObjectiveIds = completedIds == null ? new List<string>() : new List<string>(completedIds);
            Refresh();
        }

        public List<string> CaptureCompletedObjectives()
        {
            return new List<string>(completedObjectiveIds);
        }

        public void Refresh()
        {
            if (variableStore != null)
            {
                for (var i = 0; i < objectives.Count; i++)
                {
                    var objective = objectives[i];
                    if (objective == null || completedObjectiveIds.Contains(objective.objectiveId))
                    {
                        continue;
                    }

                    if (variableStore.EvaluateConditions(objective.completionConditions))
                    {
                        completedObjectiveIds.Add(objective.objectiveId);
                    }
                }
            }

            var nextObjective = FindFirstIncomplete();
            if (nextObjective != CurrentObjective)
            {
                CurrentObjective = nextObjective;
                OnCurrentObjectiveChanged?.Invoke(CurrentObjective);
            }
        }

        private ObjectiveDefinition FindFirstIncomplete()
        {
            for (var i = 0; i < objectives.Count; i++)
            {
                var objective = objectives[i];
                if (objective == null)
                {
                    continue;
                }

                if (!completedObjectiveIds.Contains(objective.objectiveId))
                {
                    return objective;
                }
            }

            return null;
        }

        private void HandleVariableChanged(string _)
        {
            Refresh();
        }
    }
}
