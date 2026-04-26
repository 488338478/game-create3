using UnityEngine;

namespace GameCreate3.DualWorld
{
    public sealed class ChatTaskController : MonoBehaviour
    {
        [SerializeField] private ChatTaskPanelUI panel;
        [SerializeField] private DualWorldWorkspace workspace;

        public enum Event { Published, Failed, Blocked, Enhanced, Completed }

        private ChatTaskDefinition activeTask;

        private void OnEnable()
        {
            if (workspace != null)
            {
                workspace.EventBus.EventRaised += HandleCrossWorldEvent;
            }
        }

        private void OnDisable()
        {
            if (workspace != null)
            {
                workspace.EventBus.EventRaised -= HandleCrossWorldEvent;
            }
        }

        public void Publish(ChatTaskDefinition task)
        {
            activeTask = task;
            Raise(Event.Published);
        }

        public void Raise(Event evt)
        {
            if (activeTask == null || panel == null)
            {
                return;
            }

            switch (evt)
            {
                case Event.Published:
                    panel.Show(activeTask.title, activeTask.initialMessage, ChatTaskPanelUI.Mood.Neutral);
                    Debug.Log($"[ChatTask] Published: {activeTask.taskId}");
                    break;
                case Event.Failed:
                    panel.Show(activeTask.title, activeTask.failureMessage, ChatTaskPanelUI.Mood.Reject);
                    Debug.Log($"[ChatTask] Failed: {activeTask.taskId}");
                    break;
                case Event.Blocked:
                    panel.Show(activeTask.title, activeTask.blockedMessage, ChatTaskPanelUI.Mood.Reject);
                    Debug.Log($"[ChatTask] Blocked: {activeTask.taskId}");
                    break;
                case Event.Enhanced:
                    panel.Show(activeTask.title, activeTask.enhancedMessage, ChatTaskPanelUI.Mood.Enhance);
                    Debug.Log($"[ChatTask] Enhanced: {activeTask.taskId}");
                    break;
                case Event.Completed:
                    panel.Show(activeTask.title, activeTask.successMessage, ChatTaskPanelUI.Mood.Approve);
                    Debug.Log($"[ChatTask] Completed: {activeTask.taskId}");
                    break;
            }
        }

        private void HandleCrossWorldEvent(CrossWorldEvent evt)
        {
            switch (evt.Type)
            {
                case CrossWorldEventType.RealityBlocked: Raise(Event.Blocked); break;
                case CrossWorldEventType.RealityEnhanced: Raise(Event.Enhanced); break;
                case CrossWorldEventType.RealityCompleted: Raise(Event.Completed); break;
            }
        }
    }
}
