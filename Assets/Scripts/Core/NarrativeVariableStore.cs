using System;
using System.Collections.Generic;
using UnityEngine;

namespace GameCreate3
{
    public sealed class NarrativeVariableStore : MonoBehaviour
    {
        [Header("Seed Variables")]
        [SerializeField] private List<BoolVariableEntry> boolVariables = new List<BoolVariableEntry>();
        [SerializeField] private List<IntVariableEntry> intVariables = new List<IntVariableEntry>();
        [SerializeField] private List<StringVariableEntry> stringVariables = new List<StringVariableEntry>();

        private readonly Dictionary<string, bool> boolMap = new Dictionary<string, bool>();
        private readonly Dictionary<string, int> intMap = new Dictionary<string, int>();
        private readonly Dictionary<string, string> stringMap = new Dictionary<string, string>();

        public event Action<string> OnVariableChanged;

        private void Awake()
        {
            RebuildCaches();
        }

        public void RebuildCaches()
        {
            boolMap.Clear();
            intMap.Clear();
            stringMap.Clear();

            for (var i = 0; i < boolVariables.Count; i++)
            {
                var entry = boolVariables[i];
                if (!string.IsNullOrWhiteSpace(entry.key))
                {
                    boolMap[entry.key] = entry.value;
                }
            }

            for (var i = 0; i < intVariables.Count; i++)
            {
                var entry = intVariables[i];
                if (!string.IsNullOrWhiteSpace(entry.key))
                {
                    intMap[entry.key] = entry.value;
                }
            }

            for (var i = 0; i < stringVariables.Count; i++)
            {
                var entry = stringVariables[i];
                if (!string.IsNullOrWhiteSpace(entry.key))
                {
                    stringMap[entry.key] = entry.value ?? string.Empty;
                }
            }
        }

        public bool HasBool(string key) => boolMap.ContainsKey(key);
        public bool HasInt(string key) => intMap.ContainsKey(key);
        public bool HasString(string key) => stringMap.ContainsKey(key);

        public bool GetBool(string key, bool defaultValue = false)
        {
            return boolMap.TryGetValue(key, out var value) ? value : defaultValue;
        }

        public int GetInt(string key, int defaultValue = 0)
        {
            return intMap.TryGetValue(key, out var value) ? value : defaultValue;
        }

        public string GetString(string key, string defaultValue = "")
        {
            return stringMap.TryGetValue(key, out var value) ? value : defaultValue;
        }

        public void SetBool(string key, bool value)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                return;
            }

            boolMap[key] = value;
            SyncBoolList(key, value);
            OnVariableChanged?.Invoke(key);
        }

        public void SetInt(string key, int value)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                return;
            }

            intMap[key] = value;
            SyncIntList(key, value);
            OnVariableChanged?.Invoke(key);
        }

        public void SetString(string key, string value)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                return;
            }

            var safeValue = value ?? string.Empty;
            stringMap[key] = safeValue;
            SyncStringList(key, safeValue);
            OnVariableChanged?.Invoke(key);
        }

        public void ApplyMutations(IReadOnlyList<DialogueVariableMutation> mutations)
        {
            if (mutations == null)
            {
                return;
            }

            for (var i = 0; i < mutations.Count; i++)
            {
                var mutation = mutations[i];
                if (mutation == null || string.IsNullOrWhiteSpace(mutation.key))
                {
                    continue;
                }

                switch (mutation.valueType)
                {
                    case NarrativeValueType.Bool:
                        SetBool(mutation.key, mutation.boolValue);
                        break;
                    case NarrativeValueType.Int:
                        if (mutation.operation == MutationOperation.Add)
                        {
                            SetInt(mutation.key, GetInt(mutation.key) + mutation.intValue);
                        }
                        else
                        {
                            SetInt(mutation.key, mutation.intValue);
                        }

                        break;
                    case NarrativeValueType.String:
                        SetString(mutation.key, mutation.stringValue);
                        break;
                }
            }
        }

        public bool EvaluateConditions(IReadOnlyList<DialogueConditionData> conditions)
        {
            if (conditions == null || conditions.Count == 0)
            {
                return true;
            }

            for (var i = 0; i < conditions.Count; i++)
            {
                if (!EvaluateCondition(conditions[i]))
                {
                    return false;
                }
            }

            return true;
        }

        public VariableSnapshot CaptureSnapshot()
        {
            return new VariableSnapshot
            {
                boolVariables = boolVariables.ToArray(),
                intVariables = intVariables.ToArray(),
                stringVariables = stringVariables.ToArray()
            };
        }

        public void RestoreSnapshot(VariableSnapshot snapshot)
        {
            if (snapshot == null)
            {
                return;
            }

            boolVariables = snapshot.boolVariables == null ? new List<BoolVariableEntry>() : new List<BoolVariableEntry>(snapshot.boolVariables);
            intVariables = snapshot.intVariables == null ? new List<IntVariableEntry>() : new List<IntVariableEntry>(snapshot.intVariables);
            stringVariables = snapshot.stringVariables == null ? new List<StringVariableEntry>() : new List<StringVariableEntry>(snapshot.stringVariables);

            RebuildCaches();
        }

        private bool EvaluateCondition(DialogueConditionData condition)
        {
            if (condition == null || string.IsNullOrWhiteSpace(condition.key))
            {
                return false;
            }

            switch (condition.comparison)
            {
                case ConditionOperator.Exists:
                    return HasAny(condition.key);
                case ConditionOperator.NotExists:
                    return !HasAny(condition.key);
            }

            switch (condition.valueType)
            {
                case NarrativeValueType.Bool:
                    return CompareBool(GetBool(condition.key), condition.boolValue, condition.comparison);
                case NarrativeValueType.Int:
                    return CompareInt(GetInt(condition.key), condition.intValue, condition.comparison);
                case NarrativeValueType.String:
                    return CompareString(GetString(condition.key), condition.stringValue, condition.comparison);
                default:
                    return false;
            }
        }

        private bool HasAny(string key)
        {
            return boolMap.ContainsKey(key) || intMap.ContainsKey(key) || stringMap.ContainsKey(key);
        }

        private static bool CompareBool(bool left, bool right, ConditionOperator comparison)
        {
            switch (comparison)
            {
                case ConditionOperator.Equals:
                    return left == right;
                case ConditionOperator.NotEquals:
                    return left != right;
                default:
                    return false;
            }
        }

        private static bool CompareInt(int left, int right, ConditionOperator comparison)
        {
            switch (comparison)
            {
                case ConditionOperator.Equals:
                    return left == right;
                case ConditionOperator.NotEquals:
                    return left != right;
                case ConditionOperator.Greater:
                    return left > right;
                case ConditionOperator.GreaterOrEqual:
                    return left >= right;
                case ConditionOperator.Less:
                    return left < right;
                case ConditionOperator.LessOrEqual:
                    return left <= right;
                default:
                    return false;
            }
        }

        private static bool CompareString(string left, string right, ConditionOperator comparison)
        {
            switch (comparison)
            {
                case ConditionOperator.Equals:
                    return string.Equals(left, right, StringComparison.Ordinal);
                case ConditionOperator.NotEquals:
                    return !string.Equals(left, right, StringComparison.Ordinal);
                default:
                    return false;
            }
        }

        private void SyncBoolList(string key, bool value)
        {
            for (var i = 0; i < boolVariables.Count; i++)
            {
                if (boolVariables[i].key == key)
                {
                    boolVariables[i] = new BoolVariableEntry { key = key, value = value };
                    return;
                }
            }

            boolVariables.Add(new BoolVariableEntry { key = key, value = value });
        }

        private void SyncIntList(string key, int value)
        {
            for (var i = 0; i < intVariables.Count; i++)
            {
                if (intVariables[i].key == key)
                {
                    intVariables[i] = new IntVariableEntry { key = key, value = value };
                    return;
                }
            }

            intVariables.Add(new IntVariableEntry { key = key, value = value });
        }

        private void SyncStringList(string key, string value)
        {
            for (var i = 0; i < stringVariables.Count; i++)
            {
                if (stringVariables[i].key == key)
                {
                    stringVariables[i] = new StringVariableEntry { key = key, value = value };
                    return;
                }
            }

            stringVariables.Add(new StringVariableEntry { key = key, value = value });
        }
    }
}
