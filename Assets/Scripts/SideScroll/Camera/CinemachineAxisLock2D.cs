using Cinemachine;
using UnityEngine;

namespace GameCreate3
{
    [SaveDuringPlay]
    public sealed class CinemachineAxisLock2D : CinemachineExtension
    {
        [SerializeField] private bool lockX;
        [SerializeField] private bool lockY;
        [SerializeField] private Vector2 lockedPosition;

        public bool LockX
        {
            get => lockX;
            set => lockX = value;
        }

        public bool LockY
        {
            get => lockY;
            set => lockY = value;
        }

        public Vector2 LockedPosition
        {
            get => lockedPosition;
            set => lockedPosition = value;
        }

        [ContextMenu("Capture Current Position")]
        public void CaptureCurrentPosition()
        {
            lockedPosition = transform.position;
        }

        protected override void PostPipelineStageCallback(
            CinemachineVirtualCameraBase vcam,
            CinemachineCore.Stage stage,
            ref CameraState state,
            float deltaTime)
        {
            if (stage != CinemachineCore.Stage.Body || (!lockX && !lockY))
            {
                return;
            }

            var position = state.RawPosition;
            if (lockX)
            {
                position.x = lockedPosition.x;
            }

            if (lockY)
            {
                position.y = lockedPosition.y;
            }

            state.RawPosition = position;
        }
    }
}
