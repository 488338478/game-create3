using UnityEngine;

namespace GameCreate3.DualWorld
{
    public enum ChatSpeaker { Npc, Player }

    public sealed class ChatLogEntry
    {
        public ChatSpeaker speaker;
        public string body;
        public ChatTaskPanelUI.Mood mood;   // NPC 用；Player 固定 Neutral
        public bool highlight;

        public ChatLogEntry(ChatSpeaker speaker, string body, ChatTaskPanelUI.Mood mood = ChatTaskPanelUI.Mood.Neutral, bool highlight = false)
        {
            this.speaker = speaker;
            this.body = body;
            this.mood = mood;
            this.highlight = highlight;
        }
    }
}
