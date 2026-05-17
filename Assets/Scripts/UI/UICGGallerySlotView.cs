using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GameCreate3.UI
{
    public sealed class UICGGallerySlotView : MonoBehaviour
    {
        [SerializeField] private TMP_Text titleLabel;
        [SerializeField] private Image thumbnailImage;
        [SerializeField] private CanvasGroup lockedGroup;
        [SerializeField] private Button button;

        private string cgId;
        private Action<string> selectedCallback;

        private void Awake()
        {
            if (button == null)
            {
                button = GetComponent<Button>();
            }
        }

        private void OnEnable()
        {
            if (button != null)
            {
                button.onClick.AddListener(HandleClicked);
            }
        }

        private void OnDisable()
        {
            if (button != null)
            {
                button.onClick.RemoveListener(HandleClicked);
            }
        }

        public void Bind(UICGGalleryItemData item, Action<string> onSelected)
        {
            cgId = item.cgId;
            selectedCallback = onSelected;

            var unlocked = item.unlocked || UICGUnlockStore.IsUnlocked(item.cgId);
            if (titleLabel != null)
            {
                titleLabel.text = unlocked ? item.title : "???";
            }

            if (thumbnailImage != null)
            {
                thumbnailImage.sprite = unlocked ? item.thumbnail : null;
                thumbnailImage.enabled = unlocked && item.thumbnail != null;
            }

            if (lockedGroup != null)
            {
                lockedGroup.alpha = unlocked ? 0f : 1f;
                lockedGroup.blocksRaycasts = !unlocked;
            }

            if (button != null)
            {
                button.interactable = unlocked;
            }
        }

        private void HandleClicked()
        {
            if (!string.IsNullOrEmpty(cgId))
            {
                selectedCallback?.Invoke(cgId);
            }
        }
    }
}
