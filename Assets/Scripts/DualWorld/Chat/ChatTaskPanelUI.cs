using UnityEngine;
using UnityEngine.UI;

namespace GameCreate3.DualWorld
{
    public sealed class ChatTaskPanelUI : MonoBehaviour
    {
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private Text titleLabel;
        [SerializeField] private Text bodyLabel;
        [SerializeField] private Image accentBar;

        [SerializeField] private Color neutralColor = new Color(0.2f, 0.25f, 0.35f);
        [SerializeField] private Color rejectColor = new Color(0.85f, 0.3f, 0.3f);
        [SerializeField] private Color enhanceColor = new Color(0.5f, 0.55f, 0.85f);
        [SerializeField] private Color approveColor = new Color(0.35f, 0.7f, 0.4f);

        public enum Mood { Neutral, Reject, Enhance, Approve }

        private void Awake()
        {
            Hide();
        }

        public void Show(string title, string body, Mood mood)
        {
            if (titleLabel != null) titleLabel.text = title;
            if (bodyLabel != null) bodyLabel.text = body;
            if (accentBar != null) accentBar.color = ResolveColor(mood);

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

        private Color ResolveColor(Mood mood)
        {
            return mood switch
            {
                Mood.Reject => rejectColor,
                Mood.Enhance => enhanceColor,
                Mood.Approve => approveColor,
                _ => neutralColor
            };
        }
    }
}
