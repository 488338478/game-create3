using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GameCreate3.UI
{
    [Serializable]
    public sealed class UIPromptPopupData
    {
        public string title;
        public string message;
        public string confirmText = "确定";
        public string cancelText = "取消";
        public bool showCancel = true;
        public Action onConfirm;
        public Action onCancel;
    }

    public sealed class UIPromptPopup : UIPageController
    {
        [SerializeField] private TMP_Text titleLabel;
        [SerializeField] private TMP_Text messageLabel;
        [SerializeField] private TMP_Text confirmLabel;
        [SerializeField] private TMP_Text cancelLabel;
        [SerializeField] private Button confirmButton;
        [SerializeField] private Button cancelButton;

        private UIPromptPopupData promptData;

        private void OnEnable()
        {
            if (confirmButton != null)
            {
                confirmButton.onClick.AddListener(HandleConfirmClicked);
            }

            if (cancelButton != null)
            {
                cancelButton.onClick.AddListener(HandleCancelClicked);
            }
        }

        private void OnDisable()
        {
            if (confirmButton != null)
            {
                confirmButton.onClick.RemoveListener(HandleConfirmClicked);
            }

            if (cancelButton != null)
            {
                cancelButton.onClick.RemoveListener(HandleCancelClicked);
            }
        }

        protected override void OnOpened(object data)
        {
            ApplyData(data);
        }

        protected override void OnRefreshed(object data)
        {
            ApplyData(data);
        }

        private void ApplyData(object data)
        {
            promptData = data as UIPromptPopupData;
            if (promptData == null)
            {
                promptData = new UIPromptPopupData
                {
                    message = data != null ? data.ToString() : string.Empty,
                    showCancel = false
                };
            }

            if (titleLabel != null)
            {
                titleLabel.text = promptData.title;
            }

            if (messageLabel != null)
            {
                messageLabel.text = promptData.message;
            }

            if (confirmLabel != null)
            {
                confirmLabel.text = string.IsNullOrEmpty(promptData.confirmText) ? "确定" : promptData.confirmText;
            }

            if (cancelLabel != null)
            {
                cancelLabel.text = string.IsNullOrEmpty(promptData.cancelText) ? "取消" : promptData.cancelText;
            }

            if (cancelButton != null)
            {
                cancelButton.gameObject.SetActive(promptData.showCancel);
            }
        }

        private void HandleConfirmClicked()
        {
            promptData?.onConfirm?.Invoke();
            CloseSelf();
        }

        private void HandleCancelClicked()
        {
            promptData?.onCancel?.Invoke();
            CloseSelf();
        }

        private void CloseSelf()
        {
            if (UIControlSystem.Instance != null)
            {
                UIControlSystem.Instance.ClosePage(PageId);
            }
            else
            {
                Close();
            }
        }
    }
}
