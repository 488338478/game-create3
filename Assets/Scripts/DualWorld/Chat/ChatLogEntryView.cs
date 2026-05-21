using UnityEngine;
using UnityEngine.UI;

namespace GameCreate3.DualWorld
{
    public sealed class ChatLogEntryView : MonoBehaviour
    {
        [Header("Roots (二选一激活)")]
        [SerializeField] private GameObject npcRoot;
        [SerializeField] private GameObject playerRoot;

        [Header("NPC 侧")]
        [SerializeField] private Text npcBody;
        [SerializeField] private Image npcMoodAccent;
        [SerializeField] private GameObject npcHighlightGlow;

        [Header("Player 侧")]
        [SerializeField] private Text playerBody;
        [SerializeField] private Image playerMoodAccent;
        [SerializeField] private GameObject playerHighlightGlow;

        public void Bind(ChatLogEntry entry, ChatTaskDefinition def, Color moodColor)
        {
            _ = def;
            var isNpc = entry.speaker == ChatSpeaker.Npc;

            if (npcRoot != null) npcRoot.SetActive(isNpc);
            if (playerRoot != null) playerRoot.SetActive(!isNpc);

            if (isNpc)
            {
                if (npcBody != null) npcBody.text = entry.body;
                if (npcMoodAccent != null) npcMoodAccent.color = moodColor;
                if (npcHighlightGlow != null) npcHighlightGlow.SetActive(entry.highlight);
            }
            else
            {
                if (playerBody != null) playerBody.text = entry.body;
                if (playerMoodAccent != null) playerMoodAccent.color = moodColor;
                if (playerHighlightGlow != null) playerHighlightGlow.SetActive(entry.highlight);
            }
        }
    }
}
