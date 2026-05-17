using System;
using System.Collections.Generic;
using UnityEngine;

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

}
