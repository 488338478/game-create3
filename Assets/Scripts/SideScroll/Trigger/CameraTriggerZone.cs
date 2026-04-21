using UnityEngine;

namespace GameCreate3
{
    public sealed class CameraTriggerZone : TriggerZoneBase
    {
        [SerializeField] private CameraConfig overrideConfig;
        [SerializeField] private bool clearOnExit = true;

        protected override void OnTriggered(Collider2D other)
        {
            Workspace?.CameraController?.ApplyZone(overrideConfig);
        }

        protected override void OnUntriggered(Collider2D other)
        {
            if (clearOnExit)
            {
                Workspace?.CameraController?.ClearZoneOverride();
            }
        }
    }
}
