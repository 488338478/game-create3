using System;
using UnityEngine;

namespace GameCreate3
{
    public enum NarrativeValueType
    {
        Bool,
        Int,
        String
    }

    public enum ConditionOperator
    {
        Equals,
        NotEquals,
        Greater,
        GreaterOrEqual,
        Less,
        LessOrEqual,
        Exists,
        NotExists
    }

    public enum MutationOperation
    {
        Set,
        Add
    }

    [Serializable]
    public sealed class DialogueConditionData
    {
        public string key = string.Empty;
        public NarrativeValueType valueType = NarrativeValueType.Bool;
        public ConditionOperator comparison = ConditionOperator.Equals;
        public bool boolValue;
        public int intValue;
        public string stringValue = string.Empty;
    }

    [Serializable]
    public sealed class DialogueVariableMutation
    {
        public string key = string.Empty;
        public NarrativeValueType valueType = NarrativeValueType.Bool;
        public MutationOperation operation = MutationOperation.Set;
        public bool boolValue;
        public int intValue;
        public string stringValue = string.Empty;
    }

    [Serializable]
    public struct BoolVariableEntry
    {
        public string key;
        public bool value;
    }

    [Serializable]
    public struct IntVariableEntry
    {
        public string key;
        public int value;
    }

    [Serializable]
    public struct StringVariableEntry
    {
        public string key;
        public string value;
    }

    [Serializable]
    public sealed class VariableSnapshot
    {
        public BoolVariableEntry[] boolVariables = Array.Empty<BoolVariableEntry>();
        public IntVariableEntry[] intVariables = Array.Empty<IntVariableEntry>();
        public StringVariableEntry[] stringVariables = Array.Empty<StringVariableEntry>();
    }
}
