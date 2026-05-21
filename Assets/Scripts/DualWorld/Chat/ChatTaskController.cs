using UnityEngine;

namespace GameCreate3.DualWorld
{
    public sealed class ChatTaskController : MonoBehaviour
    {
        public enum Event { Published, Failed, Blocked, Enhanced, Completed }

        private ChatTaskDefinition activeTask;
        private DualWorldWorkspace workspace;
        private ChatBoxUI chatBox;
        private int playerSubmitCount;

        public ChatBoxUI ChatBox => chatBox;

        private DualWorldWorkspace Workspace =>
            workspace != null ? workspace : (workspace = GetComponentInParent<DualWorldWorkspace>());

        private ChatBoxUI EnsureChatBox()
        {
            if (chatBox == null) chatBox = FindObjectOfType<ChatBoxUI>(true);
            return chatBox;
        }

        private void OnEnable()
        {
            EnsureChatBox();

            if (Workspace != null)
            {
                Workspace.EventBus.EventRaised += HandleCrossWorldEvent;
            }
            else
            {
                Debug.LogWarning("[ChatTaskController] No DualWorldWorkspace found in parent hierarchy.");
            }

            if (chatBox == null)
            {
                Debug.LogWarning("[ChatTaskController] No ChatBoxUI found in scene.");
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
            playerSubmitCount = 0;

            if (EnsureChatBox() != null) chatBox.SetTaskHeader(task);

            Raise(Event.Published);
        }

        public void Raise(Event evt)
        {
            if (activeTask == null || EnsureChatBox() == null) return;

            switch (evt)
            {
                case Event.Published:
                    AppendNpc(activeTask.initialMessage, ChatTaskPanelUI.Mood.Neutral, activeTask.highlightInitial);
                    Debug.Log($"[ChatTask] Published: {activeTask.taskId}");
                    break;
                case Event.Failed:
                    AppendNpc(activeTask.failureMessage, ChatTaskPanelUI.Mood.Reject, activeTask.highlightFailure);
                    Debug.Log($"[ChatTask] Failed: {activeTask.taskId}");
                    break;
                case Event.Blocked:
                    AppendNpc(activeTask.blockedMessage, ChatTaskPanelUI.Mood.Reject, activeTask.highlightBlocked);
                    Debug.Log($"[ChatTask] Blocked: {activeTask.taskId}");
                    break;
                case Event.Enhanced:
                    AppendNpc(activeTask.enhancedMessage, ChatTaskPanelUI.Mood.Enhance, activeTask.highlightEnhanced);
                    Debug.Log($"[ChatTask] Enhanced: {activeTask.taskId}");
                    break;
                case Event.Completed:
                    AppendNpc(activeTask.successMessage, ChatTaskPanelUI.Mood.Approve, activeTask.highlightSuccess);
                    Debug.Log($"[ChatTask] Completed: {activeTask.taskId}");
                    break;
            }
        }

        /// <summary>玩家点 submit 时调一次，append 一条玩家语料到 log 并 advance 轮换 index。</summary>
        public void AppendPlayerSubmit()
        {
            if (activeTask == null || EnsureChatBox() == null) return;
            var line = activeTask.PickPlayerLine(playerSubmitCount);
            playerSubmitCount++;
            if (!string.IsNullOrEmpty(line))
            {
                chatBox.Append(new ChatLogEntry(ChatSpeaker.Player, line));
            }
        }

        private void AppendNpc(string body, ChatTaskPanelUI.Mood mood, bool highlight)
        {
            if (string.IsNullOrEmpty(body)) return;
            chatBox.Append(new ChatLogEntry(ChatSpeaker.Npc, body, mood, highlight));
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
