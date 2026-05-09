using UnityEngine;

namespace GameCreate3.StoryPlayer
{
    /// <summary>
    /// 监听 SideScrollWorkspaceBase 抛出的指定 eventId，命中后播剧情。
    /// 用法：拖到工作区根的子节点下，eventId 字段填想监听的工作区事件名。
    /// </summary>
    public sealed class StoryWorkspaceEventTrigger : MonoBehaviour
    {
        [SerializeField] private string eventId = "story.trigger";
        [SerializeField] private StorySequence sequence;
        [SerializeField] private bool oneShot = true;

        private SideScrollWorkspaceBase workspace;
        private bool fired;

        private void OnEnable()
        {
            workspace = GetComponentInParent<SideScrollWorkspaceBase>();
            if (workspace != null) workspace.WorkspaceEventRaised += HandleEvent;
            else Debug.LogWarning("[StoryWorkspaceEventTrigger] No SideScrollWorkspaceBase in parent hierarchy.");
        }

        private void OnDisable()
        {
            if (workspace != null) workspace.WorkspaceEventRaised -= HandleEvent;
        }

        private void HandleEvent(string id)
        {
            if (oneShot && fired) return;
            if (id != eventId) return;
            if (sequence == null) return;

            fired = true;
            StoryPlayerService.Play(sequence);
        }
    }
}
