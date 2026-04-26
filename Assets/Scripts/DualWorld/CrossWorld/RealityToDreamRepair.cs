using UnityEngine;

namespace GameCreate3.DualWorld
{
    public sealed class RealityToDreamRepair : MonoBehaviour
    {
        [SerializeField] private DualWorldWorkspace workspace;
        [SerializeField] private DreamPathOpener pathOpener;

        private void OnEnable()
        {
            if (workspace != null)
            {
                workspace.EventBus.EventRaised += HandleEvent;
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
