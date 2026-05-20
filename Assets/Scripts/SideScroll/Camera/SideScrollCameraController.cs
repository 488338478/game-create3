using Cinemachine;
using UnityEngine;

namespace GameCreate3
{
    public sealed class SideScrollCameraController : MonoBehaviour
    {
        [SerializeField] private CinemachineVirtualCamera virtualCamera;
        [SerializeField] private CinemachineConfiner2D confiner2D;
        [SerializeField] private CameraConfig defaultConfig;

        private CameraConfig currentZoneConfig;
        public CameraConfig CurrentConfig => currentZoneConfig != null ? currentZoneConfig : defaultConfig;

        private void Awake()
        {
            if (virtualCamera == null)
            {
                virtualCamera = GetComponentInChildren<CinemachineVirtualCamera>(true);
            }

            if (confiner2D == null && virtualCamera != null)
            {
                confiner2D = virtualCamera.GetComponent<CinemachineConfiner2D>();
            }
        }

        public void SetFollowTarget(Transform target)
        {
            if (virtualCamera != null && virtualCamera.Follow == null)
            {
                virtualCamera.Follow = target;
            }
        }

        public void SetConfiner(Collider2D bounds)
        {
            if (confiner2D != null)
            {
                confiner2D.m_BoundingShape2D = bounds;
                confiner2D.InvalidateCache();
            }
        }

        public void ApplyCameraConfig(CameraConfig config)
        {
            defaultConfig = config;
            ApplyConfig(config);
        }

        public void ApplyZone(CameraConfig config)
        {
            currentZoneConfig = config;
            ApplyConfig(CurrentConfig);
        }

        public void ClearZoneOverride()
        {
            currentZoneConfig = null;
            ApplyConfig(CurrentConfig);
        }

        public void ResetToDefault()
        {
            currentZoneConfig = null;
            ApplyConfig(defaultConfig);
        }

        private void ApplyConfig(CameraConfig config)
        {
            if (config == null || virtualCamera == null)
            {
                return;
            }

            virtualCamera.m_Lens.OrthographicSize = config.orthographicSize;
            if (virtualCamera.GetCinemachineComponent<CinemachineFramingTransposer>() is CinemachineFramingTransposer framing)
            {
                framing.m_TrackedObjectOffset = config.followOffset;
                framing.m_XDamping = config.damping.x;
                framing.m_YDamping = config.damping.y;
            }

            if (confiner2D != null)
            {
                confiner2D.enabled = config.useConfiner && confiner2D.m_BoundingShape2D != null;
            }
        }
    }
}
