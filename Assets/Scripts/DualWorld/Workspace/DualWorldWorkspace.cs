using UnityEngine;

namespace GameCreate3.DualWorld
{
    public sealed class DualWorldWorkspace : GameCreate3.SideScrollWorkspaceBase
    {
        [SerializeField] private LevelInGameFlowController flowController;
        [SerializeField] private ChatTaskController chatTaskController;

        public CrossWorldEventBus EventBus { get; } = new CrossWorldEventBus();
        public LevelInGameFlowController FlowController => flowController;

        // chatTaskController 现在挂在 scene 驻留的 ChatBox 上（不在 workspace 子树、不能跨 prefab 拖引用），
        // 所以引用为空时运行时查一次 active 的 controller。
        public ChatTaskController ChatTaskController =>
            chatTaskController != null ? chatTaskController : (chatTaskController = FindObjectOfType<ChatTaskController>());

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
