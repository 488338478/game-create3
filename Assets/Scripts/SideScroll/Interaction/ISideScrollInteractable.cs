using UnityEngine;

namespace GameCreate3
{
    public interface ISideScrollInteractable
    {
        string Prompt { get; }
        bool CanInteract(GameObject interactor);
        void Interact(GameObject interactor);
    }
}
