using UnityEngine;

namespace GameCreate3.Level3
{
    /// <summary>
    /// 第三关工作区。继承 SideScrollWorkspaceBase，组合所有功能模块。
    /// 在 LateUpdate 中将玩家位置限制在 InvisibleWallController 的边界内。
    /// </summary>
    public sealed class Level3Workspace : SideScrollWorkspaceBase
    {
        [Header("Level 3 Modules")]
        [SerializeField] private InvisibleWallController wallController;
        [SerializeField] private Level3PhaseController phaseController;

        public InvisibleWallController WallController => wallController;
        public Level3PhaseController PhaseController => phaseController;

        protected override void Awake()
        {
            ResolveModuleReferences();
            base.Awake();
        }

        protected override void OnWorkspaceEntered()
        {
            base.OnWorkspaceEntered();

            if (phaseController != null)
                phaseController.Begin();
        }

        private void ResolveModuleReferences()
        {
            if (wallController == null)
                wallController = GetComponentInChildren<InvisibleWallController>(true);
            if (phaseController == null)
                phaseController = GetComponentInChildren<Level3PhaseController>(true);
        }

    }
}
