using UnityEngine;
using UnityEngine.UI;

namespace GameCreate3.DualWorld
{
    public sealed class ChatLogEntryView : MonoBehaviour
    {
        [SerializeField] private Text bodyLabel;
        [SerializeField] private Image avatarImage;
        [SerializeField] private Image moodAccent;
        [SerializeField] private GameObject highlightGlow;
        [SerializeField] private GameObject npcRoot;     // 可选：NPC 气泡布局
        [SerializeField] private GameObject playerRoot;  // 可选：Player 气泡布局

        public void Bind(ChatLogEntry entry, ChatTaskDefinition def, Color moodColor)
        {
            if (bodyLabel != null) bodyLabel.text = entry.body;
            if (moodAccent != null) moodAccent.color = moodColor;

            var avatar = entry.avatar;
            if (avatar == null && def != null)
            {
                avatar = entry.speaker == ChatSpeaker.Npc ? def.npcAvatar : def.playerAvatar;
            }
            if (avatarImage != null)
            {
                avatarImage.sprite = avatar;
                avatarImage.enabled = avatar != null;
            }

            if (highlightGlow != null) highlightGlow.SetActive(entry.highlight);

            // 左右气泡：speaker 不同 -> 不同 root 激活
            if (npcRoot != null) npcRoot.SetActive(entry.speaker == ChatSpeaker.Npc);
            if (playerRoot != null) playerRoot.SetActive(entry.speaker == ChatSpeaker.Player);
        }
    }
}
