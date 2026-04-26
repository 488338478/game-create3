namespace GameCreate3
{
    public sealed class SideScrollGameplayWorkspace : SideScrollWorkspaceBase
    {
        public bool IsCompleted { get; private set; }

        public void EvaluateCompletion()
        {
            IsCompleted = EvaluateConfiguredCompletion();
            if (IsCompleted)
            {
                RaiseWorkspaceEvent("workspace.completed");
            }
        }

        public void MarkCompletedFromExit()
        {
            if (!IsCompleted)
            {
                EvaluateCompletion();
            }
        }

        protected override void OnWorkspaceEntered()
        {
            EvaluateCompletion();
        }
    }
}
