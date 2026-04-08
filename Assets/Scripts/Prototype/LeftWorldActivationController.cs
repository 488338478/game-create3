using UnityEngine;

namespace GameCreate3
{
    public sealed class LeftWorldActivationController : MonoBehaviour
    {
        private SideScrollerPlayerController playerController;
        private SpriteRenderer goalRenderer;

        public void Initialize(SideScrollerPlayerController controller, SpriteRenderer goal)
        {
            playerController = controller;
            goalRenderer = goal;
            SetInteractive(false);
        }

        public void SetInteractive(bool interactive)
        {
            if (playerController != null)
            {
                playerController.SetInputLocked(!interactive);
            }

            if (goalRenderer != null)
            {
                goalRenderer.color = interactive
                    ? new Color(0.55f, 0.91f, 0.77f)
                    : new Color(0.38f, 0.55f, 0.51f);
            }
        }

        public void MarkCompleted()
        {
            if (goalRenderer != null)
            {
                goalRenderer.color = new Color(1f, 0.95f, 0.58f);
            }
        }
    }
}
