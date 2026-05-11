using System;
using System.Collections.Generic;
using GameCreate3;
using UnityEngine;

namespace GameCreate3.Core
{
    [Serializable]
    public sealed class GamePersistencePayload
    {
        public string version = "1.0.0";
        public StringEntry[] config = Array.Empty<StringEntry>();
        public StringEntry[] progress = Array.Empty<StringEntry>();
    }

    [Serializable]
    public struct StringEntry
    {
        public string key;
        public string value;
    }

    public sealed class GameSaveProgressService : MonoBehaviour
    {
        public const string DefaultSaveFileName = "game_persistence.json";

        [SerializeField] private bool dontDestroyOnLoad = true;
        [SerializeField] private string saveFileName = DefaultSaveFileName;
        [SerializeField] private bool loadOnAwake = true;

        private readonly Dictionary<string, string> configMap =
            new Dictionary<string, string>(StringComparer.Ordinal);

        private readonly Dictionary<string, string> progressMap =
            new Dictionary<string, string>(StringComparer.Ordinal);

        public static GameSaveProgressService Instance { get; private set; }

        public string SaveFileName => saveFileName;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            if (dontDestroyOnLoad)
            {
                DontDestroyOnLoad(gameObject);
            }

            if (loadOnAwake)
            {
                Load();
            }
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        public void Save()
        {
            var payload = new GamePersistencePayload
            {
                config = DictionaryToArray(configMap),
                progress = DictionaryToArray(progressMap)
            };
            JsonSaveUtility.Save(saveFileName, payload);
        }

        public void Load()
        {
            if (!JsonSaveUtility.TryLoad(saveFileName, out GamePersistencePayload payload) || payload == null)
            {
                return;
            }

            ImportArray(payload.config, configMap);
            ImportArray(payload.progress, progressMap);
        }

        public void SetConfig(string key, string value)
        {
            if (string.IsNullOrEmpty(key))
            {
                return;
            }

            configMap[key] = value ?? string.Empty;
        }

        public string GetConfig(string key, string defaultValue = null)
        {
            if (string.IsNullOrEmpty(key))
            {
                return defaultValue;
            }

            return configMap.TryGetValue(key, out var v) ? v : defaultValue;
        }

        public void SetProgress(string key, string value)
        {
            if (string.IsNullOrEmpty(key))
            {
                return;
            }

            progressMap[key] = value ?? string.Empty;
        }

        public string GetProgress(string key, string defaultValue = null)
        {
            if (string.IsNullOrEmpty(key))
            {
                return defaultValue;
            }

            return progressMap.TryGetValue(key, out var v) ? v : defaultValue;
        }

        public bool TryGetConfigBool(string key, bool defaultValue = false)
        {
            var raw = GetConfig(key, null);
            if (raw == null)
            {
                return defaultValue;
            }

            return bool.TryParse(raw, out var b) ? b : defaultValue;
        }

        public bool TryGetProgressBool(string key, bool defaultValue = false)
        {
            var raw = GetProgress(key, null);
            if (raw == null)
            {
                return defaultValue;
            }

            return bool.TryParse(raw, out var b) ? b : defaultValue;
        }

        public void DeleteSaveFile()
        {
            JsonSaveUtility.Delete(saveFileName);
            configMap.Clear();
            progressMap.Clear();
        }

        private static StringEntry[] DictionaryToArray(Dictionary<string, string> source)
        {
            if (source == null || source.Count == 0)
            {
                return Array.Empty<StringEntry>();
            }

            var arr = new StringEntry[source.Count];
            var i = 0;
            foreach (var kv in source)
            {
                arr[i++] = new StringEntry { key = kv.Key, value = kv.Value ?? string.Empty };
            }

            return arr;
        }

        private static void ImportArray(StringEntry[] source, Dictionary<string, string> target)
        {
            target.Clear();
            if (source == null)
            {
                return;
            }

            for (var i = 0; i < source.Length; i++)
            {
                var e = source[i];
                if (!string.IsNullOrEmpty(e.key))
                {
                    target[e.key] = e.value ?? string.Empty;
                }
            }
        }
    }
}
