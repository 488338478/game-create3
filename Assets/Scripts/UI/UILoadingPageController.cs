using TMPro;
using UnityEngine;

namespace GameCreate3.UI
{
    public sealed class UILoadingPageController : UIPageController
    {
        [SerializeField] private TMP_Text loadingText;
        [SerializeField] private RectTransform spinner;

        [SerializeField] private float spinnerDegreesPerSecond = 180f;

        protected override void OnOpened(object data)
        {
            SetLoadingText(data != null ? data.ToString() : "加载中...");
        }

        private void Update()
        {
            if (spinner != null && IsOpen)
            {
                spinner.Rotate(0f, 0f, -spinnerDegreesPerSecond * Time.unscaledDeltaTime);
            }
        }

        public void SetLoadingText(string text)
        {
            if (loadingText != null)
            {
                loadingText.text = text ?? string.Empty;
            }
        }
    }
}
