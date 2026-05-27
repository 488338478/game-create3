using UnityEngine;
using UnityEngine.UI;

namespace GameCreate3.DualWorld
{
    public sealed class ChatLogEntryView : MonoBehaviour
    {
        private const float NpcBubbleMinWidth = 172f;
        private const float NpcBubbleMaxWidth = 280f;
        private const float NpcBubbleWidthPerDisplayChar = 16f;
        private const int NpcBubblePreferredLineChars = 7;
        private const int NpcBubbleTextBottomPadding = 12;
        private const int NpcBubbleStickerBottomPadding = 60;

        [Header("Roots (二选一激活)")]
        [SerializeField] private GameObject npcRoot;
        [SerializeField] private GameObject playerRoot;

        [Header("NPC 侧")]
        [SerializeField] private Text npcBody;
        [SerializeField] private Image npcSticker;

        [Header("Player 侧")]
        [SerializeField] private Text playerBody;

        private Image runtimeNpcSticker;

        public void Bind(ChatLogEntry entry, ChatTaskDefinition def)
        {
            _ = def;
            var isNpc = entry.speaker == ChatSpeaker.Npc;

            if (npcRoot != null) npcRoot.SetActive(isNpc);
            if (playerRoot != null) playerRoot.SetActive(!isNpc);

            if (isNpc)
            {
                BindNpcBody(entry);
            }
            else
            {
                if (playerBody != null) playerBody.text = entry.body;
            }
        }

        private void BindNpcBody(ChatLogEntry entry)
        {
            var hasSticker = entry.sticker != null;
            if (npcBody != null)
            {
                npcBody.gameObject.SetActive(!hasSticker);
                npcBody.text = hasSticker ? string.Empty : entry.body;
                AdjustNpcBubbleLayout(entry.body, hasSticker);
            }

            var sticker = ResolveNpcSticker();
            if (sticker == null) return;

            sticker.gameObject.SetActive(hasSticker);
            sticker.sprite = entry.sticker;
            sticker.preserveAspect = true;
        }

        private void AdjustNpcBubbleLayout(string body, bool hasSticker)
        {
            if (npcBody == null || npcBody.transform.parent == null) return;

            var bubble = npcBody.transform.parent;
            var group = bubble.GetComponent<VerticalLayoutGroup>();
            if (group != null)
            {
                group.padding.bottom = hasSticker ? NpcBubbleStickerBottomPadding : NpcBubbleTextBottomPadding;
            }

            var bubbleLayout = bubble.GetComponent<LayoutElement>();
            if (bubbleLayout == null || hasSticker) return;

            var displayChars = EstimateDisplayLineChars(body);
            bubbleLayout.preferredWidth = Mathf.Clamp(
                NpcBubbleMinWidth + displayChars * NpcBubbleWidthPerDisplayChar,
                NpcBubbleMinWidth,
                NpcBubbleMaxWidth);
        }

        private static int EstimateDisplayLineChars(string text)
        {
            if (string.IsNullOrEmpty(text)) return 0;

            var hasManualBreak = false;
            var totalChars = 0;
            var lineChars = 0;
            var widestLine = 0;
            foreach (var c in text)
            {
                if (c == '\n')
                {
                    hasManualBreak = true;
                    widestLine = Mathf.Max(widestLine, lineChars);
                    lineChars = 0;
                    continue;
                }

                if (char.IsWhiteSpace(c)) continue;

                lineChars++;
                totalChars++;
            }

            widestLine = Mathf.Max(widestLine, lineChars);
            if (hasManualBreak) return Mathf.Min(widestLine, NpcBubblePreferredLineChars);

            var estimatedLineChars = Mathf.CeilToInt(Mathf.Sqrt(totalChars * 1.8f));
            return Mathf.Clamp(estimatedLineChars, 0, NpcBubblePreferredLineChars);
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
