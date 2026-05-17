using TMPro;
using UnityEngine;

namespace GameCreate3.UI
{
    public sealed class UIStoryOverlayPageController : UIPageController
    {
        [SerializeField] private TMP_Text continueHint;
        [SerializeField] private TMP_Text skipHint;

        public void ShowContinueHint(bool show)
        {
            if (continueHint != null)
            {
                continueHint.gameObject.SetActive(show);
            }
        }

        public void ShowSkipHint(bool show)
        {
            if (skipHint != null)
            {
                skipHint.gameObject.SetActive(show);
            }
        }
    }
}
