using UnityEngine;
using UnityEngine.UI;

namespace GameCreate3
{
    public sealed class BossFeedbackPanel : MonoBehaviour
    {
        [SerializeField] private Text feedbackLabel;
        [SerializeField] private Image bubbleImage;
        [SerializeField] private Color neutralTextColor = new Color(0.96f, 0.94f, 0.88f);
        [SerializeField] private Color rejectTextColor = new Color(1f, 0.93f, 0.95f);
        [SerializeField] private Color approveTextColor = new Color(0.93f, 1f, 0.95f);
        [SerializeField] private Color neutralBubbleColor = new Color(0.18f, 0.22f, 0.29f, 1f);
        [SerializeField] private Color rejectBubbleColor = new Color(0.39f, 0.18f, 0.21f, 1f);
        [SerializeField] private Color approveBubbleColor = new Color(0.18f, 0.38f, 0.25f, 1f);

        public void Initialize(Text label, Image bubble)
        {
            feedbackLabel = label;
            bubbleImage = bubble;
        }

        public void ShowNeutral(string line)
        {
            ApplyVisual(line, neutralTextColor, neutralBubbleColor);
        }

        public void ShowReject(string line)
        {
            ApplyVisual(line, rejectTextColor, rejectBubbleColor);
        }

        public void ShowApprove(string line)
        {
            ApplyVisual(line, approveTextColor, approveBubbleColor);
        }

        private void ApplyVisual(string line, Color textColor, Color bubbleColor)
        {
            if (feedbackLabel != null)
            {
                feedbackLabel.text = line;
                feedbackLabel.color = textColor;
            }

            if (bubbleImage != null)
            {
                bubbleImage.color = bubbleColor;
            }
        }
    }
}
