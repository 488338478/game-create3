using System.Collections.Generic;
using UnityEngine;

namespace GameCreate3.DualWorld
{
    [CreateAssetMenu(menuName = "GameCreate3/DualWorld/Chat Task Definition", fileName = "ChatTaskDefinition")]
    public sealed class ChatTaskDefinition : ScriptableObject
    {
        [System.Serializable]
        public sealed class NpcChatMessage
        {
            [TextArea] public string text;
            public Sprite sticker;

            public bool HasContent => !string.IsNullOrWhiteSpace(text) || sticker != null;
        }

        public string taskId = "chat.task";
        public string title = "未命名任务";

        [Header("Initial (发布任务时立刻发一条)")]
        public NpcChatMessage initialMessage = new NpcChatMessage { text = "来，把右边那几个模块按参考摆齐。我盯着呢。" };
        public bool highlightInitial = false;

        [Header("NPC Random Messages")]
        public List<NpcChatMessage> failureMessages = new List<NpcChatMessage>
        {
            new NpcChatMessage { text = "不对，再调一下。" }
        };
        public List<NpcChatMessage> successMessages = new List<NpcChatMessage>
        {
            new NpcChatMessage { text = "这次可以了。" }
        };

        [Header("Player Lines (rotate by submit index, last loops)")]
        public List<string> playerSubmitLines = new List<string>
        {
            "你看这版？",
            "这次呢？",
            "再调了一下，看看？",
            "这样行不行？",
            "你再看看？"
        };

        [Header("Highlight per NPC event")]
        public bool highlightFailure = false;
        public bool highlightSuccess = true;

        public string PickPlayerLine(int submitIndex)
        {
            if (playerSubmitLines == null || playerSubmitLines.Count == 0) return string.Empty;
            var idx = Mathf.Clamp(submitIndex, 0, playerSubmitLines.Count - 1);
            return playerSubmitLines[idx];
        }

        public NpcChatMessage PickFailureMessage()
        {
            return PickNpcMessage(failureMessages);
        }

        public NpcChatMessage PickSuccessMessage()
        {
            return PickNpcMessage(successMessages);
        }

        private static NpcChatMessage PickNpcMessage(IReadOnlyList<NpcChatMessage> messages)
        {
            if (messages == null) return null;

            var validCount = 0;
            for (var i = 0; i < messages.Count; i++)
            {
                if (messages[i] != null && messages[i].HasContent) validCount++;
            }

            if (validCount == 0) return null;

            var target = Random.Range(0, validCount);
            for (var i = 0; i < messages.Count; i++)
            {
                var message = messages[i];
                if (message == null || !message.HasContent) continue;
                if (target == 0) return message;
                target--;
            }

            return null;
        }
    }
}
