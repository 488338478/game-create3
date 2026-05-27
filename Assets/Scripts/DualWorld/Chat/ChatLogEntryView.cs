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
        [SerializeField] private Image npcSticker;
        [SerializeField] private Image npcMoodAccent;
        [SerializeField] private GameObject npcHighlightGlow;

        [Header("Player 侧")]
        [SerializeField] private Text playerBody;
        [SerializeField] private Image playerMoodAccent;
        [SerializeField] private GameObject playerHighlightGlow;

        private Image runtimeNpcSticker;

        public void Bind(ChatLogEntry entry, ChatTaskDefinition def, Color moodColor)
        {
            _ = def;
            var isNpc = entry.speaker == ChatSpeaker.Npc;

            if (npcRoot != null) npcRoot.SetActive(isNpc);
            if (playerRoot != null) playerRoot.SetActive(!isNpc);

            if (isNpc)
            {
                BindNpcBody(entry);
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

        private void BindNpcBody(ChatLogEntry entry)
        {
            var hasSticker = entry.sticker != null;
            if (npcBody != null)
            {
                npcBody.gameObject.SetActive(!hasSticker);
                npcBody.text = hasSticker ? string.Empty : entry.body;
            }

            var sticker = ResolveNpcSticker();
            if (sticker == null) return;

            sticker.gameObject.SetActive(hasSticker);
            sticker.sprite = entry.sticker;
            sticker.preserveAspect = true;
        }

        private Image ResolveNpcSticker()
        {
            if (npcSticker != null) return npcSticker;
            if (runtimeNpcSticker != null) return runtimeNpcSticker;
            if (npcBody == null || npcBody.transform.parent == null) return null;

            var go = new GameObject("NpcSticker", typeof(RectTransform), typeof(Image), typeof(LayoutElement));
            go.transform.SetParent(npcBody.transform.parent, false);
            runtimeNpcSticker = go.GetComponent<Image>();
            runtimeNpcSticker.raycastTarget = false;

            var layout = go.GetComponent<LayoutElement>();
            layout.preferredWidth = 96f;
            layout.preferredHeight = 96f;

            return runtimeNpcSticker;
        }
    }
}
