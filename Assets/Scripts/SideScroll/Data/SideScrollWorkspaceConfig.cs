using UnityEngine;

namespace GameCreate3
{
    [CreateAssetMenu(menuName = "GameCreate3/SideScroll/Workspace Config", fileName = "SideScrollWorkspaceConfig")]
    public sealed class SideScrollWorkspaceConfig : ScriptableObject
    {
        public string workspaceId = "workspace.default";
        public WorkspaceMode mode = WorkspaceMode.Generic;
        public GameObject playerPrefab;
        public bool inputEnabledOnEnter = true;
        public bool autoCompleteOnExit;
        public bool exposeCompletionEvent = true;
        public CameraConfig defaultCameraConfig;
        public CharacterMoveConfig moveConfig;
        public CharacterJumpConfig jumpConfig;
        public string[] requiredEventIds = new string[0];
        public string[] requiredPickupIds = new string[0];
        public string[] requiredGoalIds = new string[0];
    }
}
