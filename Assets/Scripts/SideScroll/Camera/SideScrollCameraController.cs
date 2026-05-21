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
            ResolveVirtualCamera();
            EnsureConfinerBinding();
        }

        private void Start()
        {
            EnsureConfinerBinding();
            if (currentZoneConfig == null && defaultConfig != null)
            {
                ApplyConfig(defaultConfig);
            }
        }

        public void SetFollowTarget(Transform target)
        {
            if (virtualCamera != null && target != null)
            {
                virtualCamera.Follow = target;
            }
        }

        public void EnsureConfinerBinding()
        {
            ResolveVirtualCamera();
            if (confiner2D == null && virtualCamera != null)
            {
                confiner2D = virtualCamera.GetComponent<CinemachineConfiner2D>();
            }

            if (confiner2D == null)
            {
                return;
            }

            if (confiner2D.m_BoundingShape2D == null)
            {
                var boundsTransform = transform.Find("CameraBounds");
                if (boundsTransform != null && boundsTransform.TryGetComponent<Collider2D>(out var bounds))
                {
                    confiner2D.m_BoundingShape2D = bounds;
                }
            }

            confiner2D.InvalidateCache();
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

            EnsureConfinerBinding();

            var orthographicSize = config.orthographicSize;
            if (config.useConfiner && confiner2D != null && confiner2D.m_BoundingShape2D != null)
            {
                orthographicSize = ClampOrthographicSizeToBounds(confiner2D.m_BoundingShape2D, orthographicSize);
            }

            virtualCamera.m_Lens.OrthographicSize = orthographicSize;
            if (virtualCamera.GetCinemachineComponent<CinemachineFramingTransposer>() is CinemachineFramingTransposer framing)
            {
                framing.m_TrackedObjectOffset = config.followOffset;
                framing.m_XDamping = config.damping.x;
                framing.m_YDamping = config.damping.y;
            }

            if (confiner2D != null)
            {
                confiner2D.enabled = config.useConfiner && confiner2D.m_BoundingShape2D != null;
                if (confiner2D.enabled)
                {
                    confiner2D.InvalidateCache();
                }
            }
        }

        private void ResolveVirtualCamera()
        {
            if (virtualCamera == null)
            {
                virtualCamera = GetComponentInChildren<CinemachineVirtualCamera>(true);
            }
        }

        private static float ClampOrthographicSizeToBounds(Collider2D bounds, float requestedSize)
        {
            if (bounds is not BoxCollider2D box)
            {
                return requestedSize;
            }

            var worldSize = Vector2.Scale(box.size, bounds.transform.lossyScale);
            var aspect = Camera.main != null ? Camera.main.aspect : 16f / 9f;
            var maxOrtho = Mathf.Min(worldSize.y * 0.5f, worldSize.x * 0.5f / aspect) * 0.98f;
            return Mathf.Min(requestedSize, maxOrtho);
        }
    }
}
