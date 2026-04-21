using System.Collections.Generic;
using UnityEngine;

namespace GameCreate3
{
    [CreateAssetMenu(fileName = "Objective_", menuName = "Game/Quest/Objective Definition")]
    public sealed class ObjectiveDefinition : ScriptableObject
    {
        public string objectiveId = "objective.intro";
        public string title = "Meet the storyteller";

        [TextArea(2, 5)]
        public string description = "Find the storykeeper in the woods and talk to them.";

        public List<DialogueConditionData> completionConditions = new List<DialogueConditionData>();
    }
}
