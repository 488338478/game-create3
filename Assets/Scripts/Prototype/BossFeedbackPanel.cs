using UnityEngine;
using UnityEngine.UI;

namespace GameCreate3
{
    public sealed class BossFeedbackPanel : MonoBehaviour
    {
        private Text feedbackLabel;
        private Image bubbleImage;

        public void Initialize(Text label, Image bubble)
        {
            feedbackLabel = label;
            bubbleImage = bubble;
        }

        public void ShowReject(string line)
        {
            if (feedbackLabel != null)
            {
                feedbackLabel.text = line;
                feedbackLabel.color = new Color(1f, 0.93f, 0.95f);
            }

            if (bubbleImage != null)
            {
                bubbleImage.color = new Color(0.39f, 0.18f, 0.21f, 1f);
            }
        }

        public void ShowApprove(string line)
        {
            if (feedbackLabel != null)
            {
                feedbackLabel.text = line;
                feedbackLabel.color = new Color(0.93f, 1f, 0.95f);
            }

            if (bubbleImage != null)
            {
                bubbleImage.color = new Color(0.18f, 0.38f, 0.25f, 1f);
            }
        }
    }
}
