using System.Collections;
using UnityEngine;

namespace GameCreate3.DualWorld
{
    public sealed class ChatTaskController : MonoBehaviour
    {
        public enum Event { Failed, Completed }

        // 同一条 NpcChatMessage 拆成「文字 + 表情」两条发时，两条之间的间隔。
        [SerializeField] private float npcMessageGapSec = 0.5f;

        private ChatTaskDefinition activeTask;
        private DualWorldWorkspace workspace;
        private ChatBoxUI chatBox;
        private int playerSubmitCount;
        private bool subscribed;

        public ChatBoxUI ChatBox => chatBox;

        // controller 现在挂在 ChatBox（不在 workspace 子树），父级找不到就全场景查。
        private DualWorldWorkspace Workspace =>
            workspace != null ? workspace : (workspace = GetComponentInParent<DualWorldWorkspace>() ?? FindObjectOfType<DualWorldWorkspace>());

        private ChatBoxUI EnsureChatBox()
        {
            if (chatBox == null) chatBox = FindObjectOfType<ChatBoxUI>(true);
            return chatBox;
        }

        private void OnEnable()
        {
            EnsureChatBox();
            EnsureSubscribed();

            if (chatBox == null)
            {
                Debug.LogWarning("[ChatTaskController] No ChatBoxUI found in scene.");
            }
        }

        private void OnDisable()
        {
            if (subscribed && workspace != null)
            {
                workspace.EventBus.EventRaised -= HandleCrossWorldEvent;
                subscribed = false;
            }
        }

        // ChatBox 驻留 scene、可能早于 workspace 实例化，所以订阅做成懒操作：
        // OnEnable 试一次，Publish（此时 workspace 一定 ready）再兜一次。
        private void EnsureSubscribed()
        {
            if (subscribed) return;
            var ws = Workspace;
            if (ws == null) return;
            ws.EventBus.EventRaised += HandleCrossWorldEvent;
            subscribed = true;
        }

        public void Publish(ChatTaskDefinition task)
        {
            EnsureSubscribed();
            activeTask = task;
            playerSubmitCount = 0;

            if (EnsureChatBox() == null) return;
            chatBox.SetTaskHeader(task);

            // 发布任务即放开场白。
            AppendNpc(task.initialMessage, ChatTaskPanelUI.Mood.Neutral, task.highlightInitial);
        }

        public void Raise(Event evt)
        {
            if (activeTask == null || EnsureChatBox() == null) return;

            switch (evt)
            {
                case Event.Failed:
                    AppendNpc(activeTask.PickFailureMessage(), ChatTaskPanelUI.Mood.Reject, activeTask.highlightFailure);
                    Debug.Log($"[ChatTask] Failed: {activeTask.taskId}");
                    break;
                case Event.Completed:
                    AppendNpc(activeTask.PickSuccessMessage(), ChatTaskPanelUI.Mood.Approve, activeTask.highlightSuccess);
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

        private void AppendNpc(ChatTaskDefinition.NpcChatMessage message, ChatTaskPanelUI.Mood mood, bool highlight)
        {
            if (message == null || !message.HasContent) return;

            // text 与 sticker 都填时，拆成两条发：先文字气泡，表情气泡隔 npcMessageGapSec 再发。
            // 渲染层是"有 sticker 就只显示表情"的二选一，所以必须分条才能同时看到文字和表情。
            var hasText = !string.IsNullOrWhiteSpace(message.text);
            if (hasText)
            {
                chatBox.Append(new ChatLogEntry(ChatSpeaker.Npc, message.text, mood, highlight));
            }

            if (message.sticker == null) return;

            if (hasText && isActiveAndEnabled && npcMessageGapSec > 0f)
            {
                StartCoroutine(AppendStickerDelayed(message.sticker, mood, highlight));
            }
            else
            {
                chatBox.Append(new ChatLogEntry(ChatSpeaker.Npc, string.Empty, mood, highlight, message.sticker));
            }
        }

        private IEnumerator AppendStickerDelayed(Sprite sticker, ChatTaskPanelUI.Mood mood, bool highlight)
        {
            yield return new WaitForSeconds(npcMessageGapSec);
            if (EnsureChatBox() != null)
            {
                chatBox.Append(new ChatLogEntry(ChatSpeaker.Npc, string.Empty, mood, highlight, sticker));
            }
        }

        private void HandleCrossWorldEvent(CrossWorldEvent evt)
        {
            switch (evt.Type)
            {
                case CrossWorldEventType.RealityCompleted: Raise(Event.Completed); break;
            }
        }
    }
}
