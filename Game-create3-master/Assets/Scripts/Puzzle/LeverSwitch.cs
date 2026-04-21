using UnityEngine;
using UnityEngine.Events;

namespace GameCreate3
{
    public enum LeverMode
    {
        Toggle,
        SetTrue,
        SetFalse
    }

    public sealed class LeverSwitch : InteractableBase
    {
        [SerializeField] private NarrativeVariableStore variableStore;
        [SerializeField] private string variableKey = "puzzle.lever_a";
        [SerializeField] private LeverMode mode = LeverMode.Toggle;
        [SerializeField] private UnityEvent<bool> onValueChanged;

        protected override void OnInteract(GameObject interactor)
        {
            if (variableStore == null || string.IsNullOrWhiteSpace(variableKey))
            {
                Debug.LogWarning("[LeverSwitch] Missing variable store or variable key.");
                return;
            }

            var currentValue = variableStore.GetBool(variableKey, false);
            var nextValue = mode switch
            {
                LeverMode.SetTrue => true,
                LeverMode.SetFalse => false,
                _ => !currentValue
            };

            variableStore.SetBool(variableKey, nextValue);
            onValueChanged?.Invoke(nextValue);
        }
    }
}
