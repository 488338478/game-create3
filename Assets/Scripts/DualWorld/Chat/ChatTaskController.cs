using UnityEngine;

namespace GameCreate3.DualWorld
{
    public sealed class ChatTaskController : MonoBehaviour
    {
        // panel 优先用 Inspector 拖；没拖时 Awake 时 GetComponentInChildren 自查。
        // workspace 完全去 SerializeField，运行时 GetComponentInParent 懒查 —— 让本组件能独立 prefab。
        [SerializeField] private ChatTaskPanelUI panel;

        public enum Event { Published, Failed, Blocked, Enhanced, Completed }

        private ChatTaskDefinition activeTask;
        private DualWorldWorkspace workspace;
        private DualWorldWorkspace Workspace =>
            workspace != null ? workspace : (workspace = GetComponentInParent<DualWorldWorkspace>());

        private void Awake()
        {
            // panel 自查：允许把 ChatTaskController 放在 ChatTaskPanel 同一棵子树里，自动找到 panel。
            if (panel == null) panel = GetComponentInChildren<ChatTaskPanelUI>(true);
            if (panel == null) panel = GetComponentInParent<Transform>()?.GetComponentInChildren<ChatTaskPanelUI>(true);
        }

        private void OnEnable()
        {
            if (Workspace != null)
            {
                Workspace.EventBus.EventRaised += HandleCrossWorldEvent;
            }
            else
            {
                Debug.LogWarning("[ChatTaskController] No DualWorldWorkspace found in parent hierarchy.");
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
