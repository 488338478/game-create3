using Cinemachine;
using UnityEngine;

namespace GameCreate3
{
    public sealed class SideScrollCameraController : MonoBehaviour
    {
        [SerializeField] private CinemachineVirtualCamera virtualCamera;
        [SerializeField] private CinemachineConfiner2D confiner2D;
        [SerializeField] private CameraConfig defaultConfig;
        [Header("Edge-adaptive follow")]
        [SerializeField, Range(0f, 0.4f)] private float deadZoneWidth = 0.18f;
        [SerializeField, Range(0f, 0.4f)] private float deadZoneHeight = 0.12f;
        [SerializeField, Range(0.4f, 1f)] private float softZoneWidth = 0.82f;
        [SerializeField, Range(0.4f, 1f)] private float softZoneHeight = 0.82f;
        [Header("Scene Overrides")]
        [SerializeField] private bool lockVerticalFollowToInitialTarget;

        private CameraConfig currentZoneConfig;
        private Transform originalFollowTarget;
        private Transform verticalFollowProxy;
        private bool verticalLockCaptured;
        private float lockedFollowY;

        public CameraConfig CurrentConfig => currentZoneConfig != null ? currentZoneConfig : defaultConfig;
        public CinemachineVirtualCamera VirtualCamera
        {
            get
            {
                ResolveVirtualCamera();
                return virtualCamera;
            }
        }
        public Collider2D BoundingShape
        {
            get
            {
                EnsureConfinerBinding();
                return confiner2D != null ? confiner2D.m_BoundingShape2D : null;
            }
        }

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

            RefreshFollowBinding();
        }

        private void LateUpdate()
        {
            RefreshFollowBinding();
        }

        public void SetFollowTarget(Transform target)
        {
            ResolveVirtualCamera();
            originalFollowTarget = target;
            verticalLockCaptured = false;

            if (virtualCamera == null)
            {
                return;
            }

            if (!lockVerticalFollowToInitialTarget)
            {
                if (virtualCamera.Follow == null || virtualCamera.Follow == verticalFollowProxy)
                {
                    virtualCamera.Follow = target;
                }
                return;
            }

            if (target == null)
            {
                if (virtualCamera.Follow == verticalFollowProxy)
                {
                    virtualCamera.Follow = null;
                }
                return;
            }

            EnsureVerticalFollowProxy();
            CaptureVerticalLock(target);
            UpdateVerticalFollowProxy();
            virtualCamera.Follow = verticalFollowProxy;
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
                // Avoid hard-centering the player near map edges. Let the target drift within
                // dead/soft zones so the confiner can take control cleanly at bounds.
                framing.m_DeadZoneWidth = deadZoneWidth;
                framing.m_DeadZoneHeight = deadZoneHeight;
                framing.m_UnlimitedSoftZone = false;
                framing.m_SoftZoneWidth = softZoneWidth;
                framing.m_SoftZoneHeight = softZoneHeight;
                framing.m_CenterOnActivate = false;
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

        private void RefreshFollowBinding()
        {
            ResolveVirtualCamera();
            if (virtualCamera == null || !lockVerticalFollowToInitialTarget)
            {
                return;
            }

            if (originalFollowTarget == null && virtualCamera.Follow != null && virtualCamera.Follow != verticalFollowProxy)
            {
                originalFollowTarget = virtualCamera.Follow;
                verticalLockCaptured = false;
            }

            if (originalFollowTarget == null)
            {
                return;
            }

            EnsureVerticalFollowProxy();
            if (!verticalLockCaptured)
            {
                CaptureVerticalLock(originalFollowTarget);
            }

            UpdateVerticalFollowProxy();
            if (virtualCamera.Follow != verticalFollowProxy)
            {
                virtualCamera.Follow = verticalFollowProxy;
            }
        }

        private void EnsureVerticalFollowProxy()
        {
            if (verticalFollowProxy != null)
            {
                return;
            }

            var proxyObject = new GameObject("VerticalFollowProxy");
            proxyObject.transform.SetParent(transform, false);
            verticalFollowProxy = proxyObject.transform;
        }

        private void CaptureVerticalLock(Transform target)
        {
            if (target == null)
            {
                return;
            }

            lockedFollowY = target.position.y;
            verticalLockCaptured = true;
        }

        private void UpdateVerticalFollowProxy()
        {
            if (verticalFollowProxy == null || originalFollowTarget == null)
            {
                return;
            }

            var targetPosition = originalFollowTarget.position;
            verticalFollowProxy.position = new Vector3(targetPosition.x, lockedFollowY, targetPosition.z);
        }

        private static float ClampOrthographicSizeToBounds(Collider2D bounds, float requestedSize)
        {
            if (bounds == null)
            {
                return requestedSize;
            }

            var worldSize = bounds.bounds.size;
            var aspect = Camera.main != null ? Camera.main.aspect : 16f / 9f;
            var maxOrtho = Mathf.Min(worldSize.y * 0.5f, worldSize.x * 0.5f / aspect) * 0.98f;
            return Mathf.Min(requestedSize, maxOrtho);
        }
    }
}
