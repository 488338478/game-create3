using System;
using System.Collections.Generic;
using UnityEngine;

namespace GameCreate3.Core
{
    public static class ResourceConfigService
    {
        private static readonly Dictionary<string, List<UnityEngine.Object>> PreloadGroups =
            new Dictionary<string, List<UnityEngine.Object>>(StringComparer.Ordinal);

        public static string LoadConfig(string configId)
        {
            if (string.IsNullOrEmpty(configId))
            {
                return null;
            }

            var textAsset = Resources.Load<TextAsset>($"GameConfigs/{configId}");
            return textAsset != null ? textAsset.text : null;
        }

        public static T LoadAsset<T>(string assetId) where T : UnityEngine.Object
        {
            if (string.IsNullOrEmpty(assetId))
            {
                return null;
            }

            return Resources.Load<T>($"GameAssets/{assetId}");
        }

        public static void Preload(string groupId)
        {
            if (string.IsNullOrEmpty(groupId))
            {
                return;
            }

            Unload(groupId);
            var loaded = Resources.LoadAll<UnityEngine.Object>($"PreloadGroups/{groupId}");
            if (loaded == null || loaded.Length == 0)
            {
                PreloadGroups[groupId] = new List<UnityEngine.Object>();
                return;
            }

            PreloadGroups[groupId] = new List<UnityEngine.Object>(loaded);
        }

        public static void Unload(string groupId)
        {
            if (string.IsNullOrEmpty(groupId))
            {
                return;
            }

            if (!PreloadGroups.TryGetValue(groupId, out var list))
            {
                return;
            }

            PreloadGroups.Remove(groupId);
            for (var i = 0; i < list.Count; i++)
            {
                var obj = list[i];
                if (obj == null)
                {
                    continue;
                }

                Resources.UnloadAsset(obj);
            }

            list.Clear();
        }
    }
}
