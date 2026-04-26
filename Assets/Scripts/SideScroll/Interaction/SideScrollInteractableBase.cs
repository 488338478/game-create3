using UnityEngine;

namespace GameCreate3
{
    [RequireComponent(typeof(Collider2D))]
    public abstract class SideScrollInteractableBase : MonoBehaviour, ISideScrollInteractable
    {
        [SerializeField] private string prompt = "Interact";

        protected SideScrollWorkspaceBase Workspace { get; private set; }
        public string Prompt => prompt;

        public virtual void BindWorkspace(SideScrollWorkspaceBase workspace)
        {
            Workspace = workspace;
            OnRegisteredToWorkspace(workspace);
        }

        public virtual bool CanInteract(GameObject interactor)
        {
            return enabled && gameObject.activeInHierarchy;
        }

        public abstract void Interact(GameObject interactor);

        protected virtual void OnRegisteredToWorkspace(SideScrollWorkspaceBase workspace)
        {
        }

        protected bool TryGetWorkspace(out SideScrollWorkspaceBase workspace)
        {
            workspace = Workspace;
            return workspace != null;
        }
    }
}
