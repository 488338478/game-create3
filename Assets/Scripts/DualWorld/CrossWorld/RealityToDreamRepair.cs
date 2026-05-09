using UnityEngine;

namespace GameCreate3.DualWorld
{
    public sealed class RealityToDreamRepair : MonoBehaviour
    {
        // workspace 改为运行时懒查 —— 见 DreamToRealityEnhancer 同款理由。
        [SerializeField] private DreamPathOpener pathOpener;

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
                Debug.LogWarning("[RealityToDreamRepair] No DualWorldWorkspace found in parent hierarchy.");
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
            if (evt.Type != CrossWorldEventType.RealityCompleted || pathOpener == null)
            {
                return;
            }

            pathOpener.OpenPath();
            workspace?.EventBus.Raise(new CrossWorldEvent(CrossWorldEventType.DreamWorldResolved, evt.SubLevelId, null));
        }
    }
}
