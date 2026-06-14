using UnityEngine;
using UnityEngine.UI;

namespace GameCreate3.DualWorld
{
    public sealed class ChatLogEntryView : MonoBehaviour
    {
        private const float BubbleMinWidth = 172f;
        private const float BubbleMaxWidth = 280f;
        private const int NpcBubbleTextBottomPadding = 16;
        // 表情气泡：表情显示高度 + 四周对称内边距（气泡按表情尺寸严丝合缝）。
        private const float StickerDisplayHeight = 110f;
        private const int StickerBubblePadding = 14;

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
            else if (playerBody != null)
            {
                playerBody.text = entry.body;
                FitTextBubble(playerBody);
            }
        }

        private void BindNpcBody(ChatLogEntry entry)
        {
            var hasSticker = entry.sticker != null;
            if (npcBody != null)
            {
                npcBody.gameObject.SetActive(!hasSticker);
                npcBody.text = hasSticker ? string.Empty : entry.body;

                if (!hasSticker)
                {
                    SetBubbleBottomPadding(npcBody, NpcBubbleTextBottomPadding);
                    FitTextBubble(npcBody);
                }
            }

            var sticker = ResolveNpcSticker();
            if (sticker == null) return;

            sticker.gameObject.SetActive(hasSticker);
            if (hasSticker)
            {
                sticker.sprite = entry.sticker;
                sticker.preserveAspect = true;
                FitStickerBubble(sticker);
            }
        }

        private static void SetBubbleBottomPadding(Text body, int bottom)
        {
            if (body == null || body.transform.parent == null) return;
            var group = body.transform.parent.GetComponent<VerticalLayoutGroup>();
            if (group != null) group.padding.bottom = bottom;
        }

        /// <summary>
        /// 让气泡真正贴合文字（保留 Body 的 localScale）。
        ///
        /// 关键点：UGUI 的 LayoutGroup / ContentSizeFitter / LayoutElement 全部无视 localScale，
        /// 所以只要让布局去控制带缩放的 Body，气泡就会按"未缩放"尺寸算大小，必然多出 1/scale 的空白。
        /// 这里改为：
        ///   1) 关掉气泡这层的 VerticalLayoutGroup，让它别再拉伸 / 摆放 Body；
        ///   2) 按文字真实字形尺寸（未缩放）测量，自己给 Body 定宽高、左上角对齐；
        ///   3) 可见尺寸 = 文字尺寸 × scale + 内边距，写进气泡的 LayoutElement，
        ///      外层链路（NpcRoot / ChatLogEntry）继续按这个值排版。
        /// </summary>
        private static void FitTextBubble(Text body)
        {
            if (body == null || body.transform.parent == null) return;

            var bubbleRT = body.transform.parent as RectTransform;
            if (bubbleRT == null) return;

            var layout = bubbleRT.GetComponent<LayoutElement>();
            if (layout == null) return;

            var group = bubbleRT.GetComponent<VerticalLayoutGroup>();
            var padLeft = group != null ? group.padding.left : 0;
            var padRight = group != null ? group.padding.right : 0;
            var padTop = group != null ? group.padding.top : 0;
            var padBottom = group != null ? group.padding.bottom : 0;

            // 接管 Body，禁用布局组件对它的控制。
            if (group != null) group.enabled = false;

            var bodyRT = body.rectTransform;
            var scale = bodyRT.localScale;
            var scaleX = Mathf.Approximately(scale.x, 0f) ? 1f : scale.x;
            var scaleY = Mathf.Approximately(scale.y, 0f) ? 1f : scale.y;

            // pivot / anchor 设为左上角：缩放围绕左上角发生，定位只需一个偏移。
            bodyRT.anchorMin = new Vector2(0f, 1f);
            bodyRT.anchorMax = new Vector2(0f, 1f);
            bodyRT.pivot = new Vector2(0f, 1f);

            // 1) 未缩放单行宽度（preferredWidth 用零 extents，不受当前 rect 影响）。
            var singleLineWidth = body.preferredWidth;
            // 可见最大内宽换算回未缩放空间，作为换行上限。
            var maxInnerUnscaled = (BubbleMaxWidth - padLeft - padRight) / scaleX;
            var bodyWidth = Mathf.Min(singleLineWidth, Mathf.Max(1f, maxInnerUnscaled));
            bodyRT.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, bodyWidth);

            // 2) 定宽后再测高度（preferredHeight 按当前 rect 宽度换行计算）。
            var bodyHeight = body.preferredHeight;
            bodyRT.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, bodyHeight);

            // 3) Body 左上角放到 (padLeft, -padTop)。
            bodyRT.anchoredPosition = new Vector2(padLeft, -padTop);

            // 4) 可见尺寸写进 LayoutElement，外层据此排版。
            var visibleWidth = Mathf.Clamp(
                bodyWidth * scaleX + padLeft + padRight, BubbleMinWidth, BubbleMaxWidth);
            var visibleHeight = bodyHeight * scaleY + padTop + padBottom;
            layout.preferredWidth = visibleWidth;
            layout.preferredHeight = visibleHeight;
        }

        /// <summary>
        /// 让表情气泡严丝合缝包住表情。
        /// 之前贴纸走原 VerticalLayoutGroup，气泡按 prefab 固定宽度（280）+ 大底边距排，
        /// 而表情只有 ~100px，于是四周大片留白。这里和文字气泡一样接管布局：
        /// 关掉 VLG，按表情原始宽高比定贴纸尺寸，气泡 = 表情 + 四周对称内边距。
        /// </summary>
        private static void FitStickerBubble(Image sticker)
        {
            if (sticker == null || sticker.transform.parent == null) return;

            var bubbleRT = sticker.transform.parent as RectTransform;
            if (bubbleRT == null) return;

            var group = bubbleRT.GetComponent<VerticalLayoutGroup>();
            if (group != null) group.enabled = false;

            var rt = sticker.rectTransform;
            rt.localScale = Vector3.one;
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0f, 1f);

            // 按表情原始宽高比换算显示尺寸，避免 preserveAspect 再次留白。
            var h = StickerDisplayHeight;
            var w = h;
            var spr = sticker.sprite;
            if (spr != null && spr.rect.height > 0f)
            {
                w = h * (spr.rect.width / spr.rect.height);
            }

            rt.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, w);
            rt.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, h);
            rt.anchoredPosition = new Vector2(StickerBubblePadding, -StickerBubblePadding);

            var layout = bubbleRT.GetComponent<LayoutElement>();
            if (layout != null)
            {
                layout.preferredWidth = w + StickerBubblePadding * 2;
                layout.preferredHeight = h + StickerBubblePadding * 2;
            }
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
