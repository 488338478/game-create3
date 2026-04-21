using System;

namespace GameCreate3
{
    [Serializable]
    public sealed class ConditionRequirementData
    {
        public enum RequirementKind
        {
            WorkspaceEvent = 0,
            Pickup = 1,
            Goal = 2
        }

        public RequirementKind kind = RequirementKind.WorkspaceEvent;
        public string id;
        public bool invert;
    }
}
