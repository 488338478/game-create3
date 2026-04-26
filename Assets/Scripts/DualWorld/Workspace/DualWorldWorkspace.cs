using UnityEngine;

namespace GameCreate3.DualWorld
{
    public sealed class DualWorldWorkspace : GameCreate3.SideScrollWorkspaceBase
    {
        [SerializeField] private LevelInGameFlowController flowController;
        [SerializeField] private ChatTaskController chatTaskController;

        public CrossWorldEventBus EventBus { get; } = new CrossWorldEventBus();
        public LevelInGameFlowController FlowController => flowController;
        public ChatTaskController ChatTaskController => chatTaskController;

        protected override void OnWorkspaceEntered()
        {
            base.OnWorkspaceEntered();

            if (flowController != null)
            {
                flowController.Bind(this);
                flowController.BeginFirstSubLevel();
            }
        }

        protected override void OnWorkspaceExited()
        {
            base.OnWorkspaceExited();
            EventBus.Clear();
        }
    }
}
