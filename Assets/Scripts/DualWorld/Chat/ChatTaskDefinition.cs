using System.Collections.Generic;
using UnityEngine;

namespace GameCreate3.DualWorld
{
    [CreateAssetMenu(menuName = "GameCreate3/DualWorld/Chat Task Definition", fileName = "ChatTaskDefinition")]
    public sealed class ChatTaskDefinition : ScriptableObject
    {
        public string taskId = "chat.task";
        public string title = "未命名任务";
        public string description = "请完成右屏任务。";

        [Header("NPC Messages")]
        [TextArea] public string initialMessage = "你来看看这版排得行不行？";
        [TextArea] public string failureMessage = "不对，再调一下。";
        [TextArea] public string blockedMessage = "你是不是哪里没看清？要不去走两步，换个角度。";
        [TextArea] public string enhancedMessage = "梦里好像帮你顺过了，再试一次。";
        [TextArea] public string successMessage = "这次可以了。";

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
        public bool highlightInitial = false;
        public bool highlightFailure = false;
        public bool highlightBlocked = false;
        public bool highlightEnhanced = true;
        public bool highlightSuccess = true;

        public string PickPlayerLine(int submitIndex)
        {
            if (playerSubmitLines == null || playerSubmitLines.Count == 0) return string.Empty;
            var idx = Mathf.Clamp(submitIndex, 0, playerSubmitLines.Count - 1);
            return playerSubmitLines[idx];
        }
    }
}
