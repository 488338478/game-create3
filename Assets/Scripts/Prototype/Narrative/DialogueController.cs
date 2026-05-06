using System;
using System.Collections.Generic;
using UnityEngine;

namespace GameCreate3
{
    public sealed class DialogueController : MonoBehaviour
    {
        [SerializeField] private NarrativeVariableStore variableStore;
        [SerializeField] private DialogueAsset startupDialogue;

        private DialogueAsset currentDialogue;
        private DialogueNodeData currentNode;
        private readonly List<DialogueChoiceData> availableChoices = new List<DialogueChoiceData>();
        private bool isRunning;

        public bool IsRunning => isRunning;
        public DialogueAsset CurrentDialogue => currentDialogue;
        public DialogueNodeData CurrentNode => currentNode;
        public IReadOnlyList<DialogueChoiceData> AvailableChoices => availableChoices;

        public event Action<DialogueViewModel> OnDialogueUpdated;
        public event Action<bool> OnDialogueActiveChanged;

        private void Start()
        {
            if (startupDialogue != null)
            {
                StartDialogue(startupDialogue);
            }
        }

        public void StartDialogue(DialogueAsset dialogueAsset, string startNodeId = null)
        {
            if (dialogueAsset == null)
            {
                Debug.LogWarning("[DialogueController] Cannot start dialogue: asset is null.");
                return;
            }

            currentDialogue = dialogueAsset;
            isRunning = true;
            OnDialogueActiveChanged?.Invoke(true);

            var initialNodeId = string.IsNullOrWhiteSpace(startNodeId) ? dialogueAsset.StartNodeId : startNodeId;
            MoveToNode(initialNodeId);
        }

        public void SelectChoice(int index)
        {
            if (!isRunning || index < 0 || index >= availableChoices.Count)
            {
                return;
            }

            var selected = availableChoices[index];
            variableStore?.ApplyMutations(selected.selectMutations);

            if (selected.endDialogue || string.IsNullOrWhiteSpace(selected.nextNodeId))
            {
                EndDialogue();
                return;
            }

            MoveToNode(selected.nextNodeId);
        }

        public void EndDialogue()
        {
            if (!isRunning)
            {
                return;
            }

            isRunning = false;
            currentDialogue = null;
            currentNode = null;
            availableChoices.Clear();
            OnDialogueActiveChanged?.Invoke(false);
        }

        private void MoveToNode(string nodeId)
        {
            if (currentDialogue == null || string.IsNullOrWhiteSpace(nodeId))
            {
                EndDialogue();
                return;
            }

            if (!currentDialogue.TryGetNode(nodeId, out var node))
            {
                Debug.LogWarning($"[DialogueController] Node '{nodeId}' was not found.");
                EndDialogue();
                return;
            }

            currentNode = node;
            variableStore?.ApplyMutations(node.enterMutations);
            BuildChoices(node.choices);
            PublishViewModel();
        }

        private void BuildChoices(IReadOnlyList<DialogueChoiceData> choices)
        {
            availableChoices.Clear();
            if (choices == null)
            {
                return;
            }

            for (var i = 0; i < choices.Count; i++)
            {
                var choice = choices[i];
                if (choice == null)
                {
                    continue;
                }

                if (variableStore == null || variableStore.EvaluateConditions(choice.conditions))
                {
                    availableChoices.Add(choice);
                }
            }
        }

        private void PublishViewModel()
        {
            var uiChoices = new List<DialogueChoiceViewModel>(availableChoices.Count);
            for (var i = 0; i < availableChoices.Count; i++)
            {
                uiChoices.Add(new DialogueChoiceViewModel(i, availableChoices[i].text));
            }

            var viewModel = new DialogueViewModel(
                currentNode != null ? currentNode.speaker : string.Empty,
                currentNode != null ? currentNode.body : string.Empty,
                uiChoices);

            OnDialogueUpdated?.Invoke(viewModel);

            // Nodes without choices are treated as end-of-node and will close on next input flow.
            if (availableChoices.Count == 0)
            {
                EndDialogue();
            }
        }
    }
}
