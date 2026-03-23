using UnityEngine;
using UnityEngine.Events;

namespace GameCreate3
{
    public sealed class PuzzleGate : InteractableBase
    {
        [SerializeField] private NarrativeVariableStore variableStore;
        [SerializeField] private DialogueConditionData[] unlockConditions;
        [SerializeField] private bool autoUnlockOnStart;
        [SerializeField] private UnityEvent onUnlocked;
        [SerializeField] private UnityEvent onLockedAttempt;

        private bool unlocked;
        public bool IsUnlocked => unlocked;

        private void Start()
        {
            if (autoUnlockOnStart)
            {
                EvaluateAndUnlock();
            }
        }

        protected override void OnInteract(GameObject interactor)
        {
            if (EvaluateAndUnlock())
            {
                return;
            }

            onLockedAttempt?.Invoke();
        }

        private bool EvaluateAndUnlock()
        {
            if (unlocked)
            {
                return true;
            }

            if (variableStore == null)
            {
                Debug.LogWarning("[PuzzleGate] Variable store is missing.");
                return false;
            }

            if (!variableStore.EvaluateConditions(unlockConditions))
            {
                return false;
            }

            unlocked = true;
            onUnlocked?.Invoke();
            return true;
        }
    }
}
