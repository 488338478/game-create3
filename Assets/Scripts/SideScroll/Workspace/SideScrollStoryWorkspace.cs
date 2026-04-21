namespace GameCreate3
{
    public sealed class SideScrollStoryWorkspace : SideScrollWorkspaceBase
    {
        public void SetStoryInputLocked(bool locked)
        {
            PlayerController?.SetInputEnabled(!locked);
        }
    }
}
