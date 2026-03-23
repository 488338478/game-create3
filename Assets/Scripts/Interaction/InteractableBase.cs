using UnityEngine;
using UnityEngine.Events;

namespace GameCreate3
{
    public class InteractableBase : MonoBehaviour, IInteractable
    {
        [SerializeField] private string prompt = "Interact";
        [SerializeField] private bool oneShot;
        [SerializeField] private UnityEvent onInteracted;

        private bool consumed;
        public string Prompt => prompt;

        public virtual bool CanInteract(GameObject interactor)
        {
            return !oneShot || !consumed;
        }

        public void Interact(GameObject interactor)
        {
            if (!CanInteract(interactor))
            {
                return;
            }

            OnInteract(interactor);
            onInteracted?.Invoke();
            if (oneShot)
            {
                consumed = true;
            }
        }

        protected virtual void OnInteract(GameObject interactor)
        {
        }
    }
}
