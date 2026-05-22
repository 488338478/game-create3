using System;
using System.Collections.Generic;
using UnityEngine;

namespace GameCreate3
{
    [CreateAssetMenu(fileName = "Dialogue_", menuName = "GameCreate3/Narrative/Dialogue Asset")]
    public sealed class DialogueAsset : ScriptableObject
    {
        [SerializeField] private string startNodeId = "start";
        [SerializeField] private List<DialogueNodeData> nodes = new List<DialogueNodeData>();

        public string StartNodeId => startNodeId;
        public IReadOnlyList<DialogueNodeData> Nodes => nodes;

        public bool TryGetNode(string nodeId, out DialogueNodeData node)
        {
            for (var i = 0; i < nodes.Count; i++)
            {
                if (nodes[i] != null && nodes[i].nodeId == nodeId)
                {
                    node = nodes[i];
                    return true;
                }
            }

            node = null;
            return false;
        }
    }

    [Serializable]
    public sealed class DialogueNodeData
    {
        public string nodeId = "start";
        public string speaker = "Narrator";

        [TextArea(2, 8)]
        public string body = "Once upon a time...";

        public List<DialogueVariableMutation> enterMutations = new List<DialogueVariableMutation>();
        public List<DialogueChoiceData> choices = new List<DialogueChoiceData>();
    }

    [Serializable]
    public sealed class DialogueChoiceData
    {
        [TextArea(1, 3)]
        public string text = "Continue";

        public string nextNodeId = string.Empty;
        public bool endDialogue;
        public List<DialogueConditionData> conditions = new List<DialogueConditionData>();
        public List<DialogueVariableMutation> selectMutations = new List<DialogueVariableMutation>();
    }
}
