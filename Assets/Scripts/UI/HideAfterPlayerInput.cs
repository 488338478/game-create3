using UnityEngine;

namespace GameCreate3.UI
{
    public sealed class HideAfterPlayerInput : MonoBehaviour
    {
        [SerializeField] private GameObject targetToHide;
        [SerializeField] private float hideDelay = 5f;
        [SerializeField] private bool detectMouseMovement = true;
        [SerializeField] private float mouseMoveThreshold = 1f;

        private bool countdownStarted;
        private float countdownRemaining;
        private Vector3 lastMousePosition;

        private void OnEnable()
        {
            countdownStarted = false;
            countdownRemaining = hideDelay;
            lastMousePosition = Input.mousePosition;
        }

        private void Update()
        {
            if (!countdownStarted)
            {
                if (HasPlayerInput())
                {
                    countdownStarted = true;
                    countdownRemaining = hideDelay;
                }

                lastMousePosition = Input.mousePosition;
                return;
            }

            countdownRemaining -= Time.unscaledDeltaTime;
            if (countdownRemaining > 0f) return;

            var target = targetToHide != null ? targetToHide : gameObject;
            target.SetActive(false);
        }

        private bool HasPlayerInput()
        {
            if (Input.anyKeyDown) return true;

            for (var i = 0; i <= 6; i++)
            {
                if (Input.GetMouseButtonDown(i)) return true;
            }

            if (Input.mouseScrollDelta.sqrMagnitude > 0f) return true;

            if (!detectMouseMovement) return false;

            var currentMousePosition = Input.mousePosition;
            var threshold = mouseMoveThreshold * mouseMoveThreshold;
            return (currentMousePosition - lastMousePosition).sqrMagnitude >= threshold;
        }
    }
}
