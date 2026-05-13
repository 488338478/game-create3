using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GameCreate3.UI
{
    [Serializable]
    public sealed class UICGGalleryItemData
    {
        public string cgId;
        public string title;
        public Sprite thumbnail;
        public bool unlocked;
    }

    [Serializable]
    public sealed class UICGGalleryData
    {
        public List<UICGGalleryItemData> items = new List<UICGGalleryItemData>();
    }

    public static class UICGUnlockStore
    {
        private const string KeyPrefix = "CG_Unlocked_";

        public static bool IsUnlocked(string cgId)
        {
            return !string.IsNullOrEmpty(cgId) && PlayerPrefs.GetInt(KeyPrefix + cgId, 0) == 1;
        }

        public static void SetUnlocked(string cgId, bool unlocked = true)
        {
            if (string.IsNullOrEmpty(cgId))
            {
                return;
            }

            PlayerPrefs.SetInt(KeyPrefix + cgId, unlocked ? 1 : 0);
            PlayerPrefs.Save();
        }
    }

    public sealed class UICGGalleryPageController : UIPageController
    {
        [SerializeField] private Transform slotContainer;
        [SerializeField] private UICGGallerySlotView slotPrefab;

        private readonly List<UICGGallerySlotView> liveSlots = new List<UICGGallerySlotView>();

        public event Action<string> OnCGSelected;

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
            var galleryData = data as UICGGalleryData;
            if (galleryData == null)
            {
                return;
            }

            Rebuild(galleryData.items);
        }

        private void Rebuild(IReadOnlyList<UICGGalleryItemData> items)
        {
            ClearSlots();
            if (slotContainer == null || slotPrefab == null || items == null)
            {
                return;
            }

            for (var i = 0; i < items.Count; i++)
            {
                var item = items[i];
                if (item == null)
                {
                    continue;
                }

                var slot = Instantiate(slotPrefab, slotContainer);
                slot.Bind(item, HandleCGSelected);
                liveSlots.Add(slot);
            }
        }

        private void ClearSlots()
        {
            for (var i = 0; i < liveSlots.Count; i++)
            {
                if (liveSlots[i] != null)
                {
                    Destroy(liveSlots[i].gameObject);
                }
            }

            liveSlots.Clear();
        }

        private void HandleCGSelected(string cgId)
        {
            OnCGSelected?.Invoke(cgId);
        }
    }

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
