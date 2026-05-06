using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GameCreate3
{
    public sealed class DialoguePanelUI : MonoBehaviour
    {
        [SerializeField] private DialogueController dialogueController;
        [SerializeField] private CanvasGroup rootGroup;
        [SerializeField] private TMP_Text speakerLabel;
        [SerializeField] private TMP_Text bodyLabel;
        [SerializeField] private Transform choiceContainer;
        [SerializeField] private Button choiceButtonPrefab;

        private readonly List<Button> liveChoiceButtons = new List<Button>();

        private void OnEnable()
        {
            if (dialogueController != null)
            {
                dialogueController.OnDialogueUpdated += HandleDialogueUpdated;
                dialogueController.OnDialogueActiveChanged += SetVisible;
            }
        }

        private void OnDisable()
        {
            if (dialogueController != null)
            {
                dialogueController.OnDialogueUpdated -= HandleDialogueUpdated;
                dialogueController.OnDialogueActiveChanged -= SetVisible;
            }
        }

        private void Start()
        {
            SetVisible(false);
        }

        private void HandleDialogueUpdated(DialogueViewModel vm)
        {
            if (speakerLabel != null)
            {
                speakerLabel.text = vm.Speaker;
            }

            if (bodyLabel != null)
            {
                bodyLabel.text = vm.Body;
            }

            RebuildChoices(vm.Choices);
        }

        private void RebuildChoices(IReadOnlyList<DialogueChoiceViewModel> choices)
        {
            for (var i = 0; i < liveChoiceButtons.Count; i++)
            {
                if (liveChoiceButtons[i] != null)
                {
                    Destroy(liveChoiceButtons[i].gameObject);
                }
            }

            liveChoiceButtons.Clear();
            if (choices == null || choiceButtonPrefab == null || choiceContainer == null)
            {
                return;
            }

            for (var i = 0; i < choices.Count; i++)
            {
                var choice = choices[i];
                var button = Instantiate(choiceButtonPrefab, choiceContainer);
                var label = button.GetComponentInChildren<TMP_Text>();
                if (label != null)
                {
                    label.text = choice.Text;
                }

                var capturedIndex = choice.Index;
                button.onClick.AddListener(() => dialogueController.SelectChoice(capturedIndex));
                liveChoiceButtons.Add(button);
            }
        }

        private void SetVisible(bool visible)
        {
            if (rootGroup == null)
            {
                gameObject.SetActive(visible);
                return;
            }

            rootGroup.alpha = visible ? 1f : 0f;
            rootGroup.interactable = visible;
            rootGroup.blocksRaycasts = visible;
        }
    }
}
