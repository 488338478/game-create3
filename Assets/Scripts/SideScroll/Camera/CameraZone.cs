using UnityEngine;

namespace GameCreate3
{
    [RequireComponent(typeof(Collider2D))]
    public sealed class CameraZone : MonoBehaviour
    {
        [SerializeField] private CameraConfig overrideConfig;
        [SerializeField] private bool restoreDefaultOnExit = true;

        private SideScrollWorkspaceBase workspace;

        public void BindWorkspace(SideScrollWorkspaceBase value)
        {
            workspace = value;
            if (TryGetComponent<Collider2D>(out var collider2D))
            {
                collider2D.isTrigger = true;
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (workspace == null || overrideConfig == null || !other.TryGetComponent<SideScrollCharacterControllerBase>(out _))
            {
                return;
            }

            workspace.CameraController?.ApplyZone(overrideConfig);
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            if (!restoreDefaultOnExit || workspace == null || !other.TryGetComponent<SideScrollCharacterControllerBase>(out _))
            {
                return;
            }

            workspace.CameraController?.ClearZoneOverride();
        }
    }
}
