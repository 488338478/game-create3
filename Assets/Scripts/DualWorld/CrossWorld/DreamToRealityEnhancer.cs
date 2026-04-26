using UnityEngine;

namespace GameCreate3.DualWorld
{
    public sealed class DreamToRealityEnhancer : MonoBehaviour
    {
        [SerializeField] private DualWorldWorkspace workspace;
        [SerializeField] private RealityAlignmentTask alignmentTask;

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
            if (evt.Type != CrossWorldEventType.DreamCompleted || alignmentTask == null)
            {
                return;
            }

            alignmentTask.SetAssistEnabled(true);
            alignmentTask.SetInteractable(true);
            workspace?.EventBus.Raise(new CrossWorldEvent(CrossWorldEventType.RealityEnhanced, evt.SubLevelId, null));
        }
    }
}
