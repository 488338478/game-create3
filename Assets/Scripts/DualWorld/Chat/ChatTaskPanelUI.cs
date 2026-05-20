using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace GameCreate3.DualWorld
{
    public sealed class ChatTaskPanelUI : MonoBehaviour
    {
        public enum Mood { Neutral, Reject, Enhance, Approve }

        [Header("Container")]
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private Text titleHeader;

        [Header("Log")]
        [SerializeField] private ScrollRect scrollRect;
        [SerializeField] private RectTransform contentRoot;
        [SerializeField] private ChatLogEntryView entryPrefab;

        [Header("Mood Palette")]
        [SerializeField] private Color neutralColor = new Color(0.2f, 0.25f, 0.35f);
        [SerializeField] private Color rejectColor = new Color(0.85f, 0.3f, 0.3f);
        [SerializeField] private Color enhanceColor = new Color(0.5f, 0.55f, 0.85f);
        [SerializeField] private Color approveColor = new Color(0.35f, 0.7f, 0.4f);

        private readonly List<ChatLogEntry> entries = new List<ChatLogEntry>();
        private ChatTaskDefinition currentDef;

        private void Awake()
        {
            Show();
        }

        public void Show()
        {
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 1f;
                canvasGroup.blocksRaycasts = true;
            }
            gameObject.SetActive(true);
        }

        public void Hide()
        {
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 0f;
                canvasGroup.blocksRaycasts = false;
            }
        }

        public void SetTaskHeader(ChatTaskDefinition def)
        {
            currentDef = def;
            if (titleHeader != null) titleHeader.text = def != null ? def.title : string.Empty;
        }

        public void Append(ChatLogEntry entry)
        {
            if (entry == null) return;
            entries.Add(entry);
            SpawnEntryView(entry);
            ScrollToBottomDeferred();
        }

        public void Clear()
        {
            entries.Clear();
            if (contentRoot != null)
            {
                for (var i = contentRoot.childCount - 1; i >= 0; i--)
                {
                    Destroy(contentRoot.GetChild(i).gameObject);
                }
            }
        }

        public Color ResolveMoodColor(Mood mood)
        {
            return mood switch
            {
                Mood.Reject => rejectColor,
                Mood.Enhance => enhanceColor,
                Mood.Approve => approveColor,
                _ => neutralColor
            };
        }

        private void SpawnEntryView(ChatLogEntry entry)
        {
            if (entryPrefab == null || contentRoot == null) return;
            var view = Instantiate(entryPrefab, contentRoot);
            view.Bind(entry, currentDef, ResolveMoodColor(entry.mood));
        }

        private void ScrollToBottomDeferred()
        {
            if (!isActiveAndEnabled) return;
            StartCoroutine(ScrollToBottomNextFrame());
        }

        private IEnumerator ScrollToBottomNextFrame()
        {
            yield return null;          // 让 layout 先 rebuild
            if (scrollRect != null) scrollRect.verticalNormalizedPosition = 0f;
        }
    }
}
