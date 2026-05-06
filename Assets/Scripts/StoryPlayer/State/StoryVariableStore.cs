using System;
using System.Collections.Generic;
using UnityEngine;

namespace GameCreate3.StoryPlayer
{
    /// <summary>
    /// StoryPlayer 内部使用的轻量变量存储。
    /// 与旧的 <c>GameCreate3.NarrativeVariableStore</c> 完全解耦：StoryPlayer 模块不依赖 Prototype 命名空间。
    /// 仅暴露 StoryEventSystem 实际需要的 Bool/Int/String setter，按需可后续扩展。
    /// </summary>
    public sealed class StoryVariableStore : MonoBehaviour
    {
        private readonly Dictionary<string, bool> boolMap = new Dictionary<string, bool>();
        private readonly Dictionary<string, int> intMap = new Dictionary<string, int>();
        private readonly Dictionary<string, string> stringMap = new Dictionary<string, string>();

        public event Action<string> OnVariableChanged;

        public bool HasBool(string key) => !string.IsNullOrEmpty(key) && boolMap.ContainsKey(key);
        public bool HasInt(string key) => !string.IsNullOrEmpty(key) && intMap.ContainsKey(key);
        public bool HasString(string key) => !string.IsNullOrEmpty(key) && stringMap.ContainsKey(key);

        public bool GetBool(string key, bool defaultValue = false)
            => boolMap.TryGetValue(key ?? string.Empty, out var v) ? v : defaultValue;

        public int GetInt(string key, int defaultValue = 0)
            => intMap.TryGetValue(key ?? string.Empty, out var v) ? v : defaultValue;

        public string GetString(string key, string defaultValue = "")
            => stringMap.TryGetValue(key ?? string.Empty, out var v) ? v : defaultValue;

        public void SetBool(string key, bool value)
        {
            if (string.IsNullOrWhiteSpace(key)) return;
            boolMap[key] = value;
            OnVariableChanged?.Invoke(key);
        }

        public void SetInt(string key, int value)
        {
            if (string.IsNullOrWhiteSpace(key)) return;
            intMap[key] = value;
            OnVariableChanged?.Invoke(key);
        }

        public void SetString(string key, string value)
        {
            if (string.IsNullOrWhiteSpace(key)) return;
            stringMap[key] = value ?? string.Empty;
            OnVariableChanged?.Invoke(key);
        }
    }
}
