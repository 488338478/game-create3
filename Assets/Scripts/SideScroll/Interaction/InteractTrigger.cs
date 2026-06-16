using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;

namespace GameCreate3
{
    public sealed class InteractTrigger : SideScrollInteractableBase
    {
        [SerializeField, FormerlySerializedAs("observationId")] private string interactId = "default";
        [SerializeField] private bool oneShot = true;

        [Header("Self Event")]
        [SerializeField, Tooltip("交互成功后触发的自事件，可在 Inspector 配置调用自身/子物体上其它脚本的方法。")]
        private UnityEvent onInteracted;

        private bool consumed;

        public string InteractId => interactId;

        public override bool CanInteract(GameObject interactor)
        {
            return base.CanInteract(interactor) && (!oneShot || !consumed);
        }

        public override void Interact(GameObject interactor)
        {
            if (!TryGetWorkspace(out var workspace))
            {
                Debug.LogWarning($"[InteractTrigger] '{name}' Interact 被调用，但 Workspace 未绑定（不在 SideScrollWorkspaceBase 的 children 下？）。interactId={interactId}", this);
                return;
            }
            if (!CanInteract(interactor))
            {
                Debug.Log($"[InteractTrigger] '{name}' CanInteract=false（oneShot 已 consumed？consumed={consumed} oneShot={oneShot}）", this);
                return;
            }

            consumed = true;
            Debug.Log($"[InteractTrigger] '{name}' raise event interact.{interactId}", this);
            workspace.RaiseWorkspaceEvent($"interact.{interactId}");
            onInteracted?.Invoke();
        }
    }
}
