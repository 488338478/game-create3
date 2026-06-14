using UnityEngine;

namespace GameCreate3.DualWorld
{
    public sealed class DreamToRealityEnhancer : MonoBehaviour
    {
        // workspace 改为运行时懒查 —— 让本组件能独立 prefab 化（不依赖 Inspector 拖引用）。
        // 唯一前提：本 GameObject 必须挂在 DualWorldWorkspace 根的子树下。
        [SerializeField] private RealityAlignmentTask alignmentTask;

        private DualWorldWorkspace workspace;
        private DualWorldWorkspace Workspace =>
            workspace != null ? workspace : (workspace = GetComponentInParent<DualWorldWorkspace>());

        private void OnEnable()
        {
            if (Workspace != null)
            {
                Workspace.EventBus.EventRaised += HandleEvent;
            }
            else
            {
                Debug.LogWarning("[DreamToRealityEnhancer] No DualWorldWorkspace found in parent hierarchy.");
            }
        }

        private void OnDisable()
        {
            if (workspace != null)
            {
                workspace.EventBus.EventRaised -= HandleEvent;
            }
        }

        private void HandleEvent(CrossWorldEvent evt)
        {
            if (evt.Type != CrossWorldEventType.DreamCompleted || alignmentTask == null)
            {
                return;
            }

            // 新机制：梦境帮助语义 = 一次性把还没解锁的 Target 全部点亮（带动画），
            // 玩家回来后只需把对应 block 拖过去吸附即可。
            alignmentTask.UnlockAllRemainingTargets();
            alignmentTask.SetAssistEnabled(true);
            alignmentTask.SetInteractable(true);
        }
    }
}
