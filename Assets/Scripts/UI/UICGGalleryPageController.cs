using System;
using System.Collections.Generic;
using UnityEngine;

namespace GameCreate3.UI
{
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
}
