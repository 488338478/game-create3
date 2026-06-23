using UnityEngine;

namespace GameCreate3.Level3
{
    public sealed class InvisibleWallController : MonoBehaviour
    {
        [Header("Bounds")]
        [SerializeField] private float initialHalfWidth = 8f;
        [SerializeField] private float minHalfWidth = 1.5f;

        [Header("Compression")]
        [SerializeField] private float compressSpeed = 0.3f;
        [SerializeField] private float hitSpeedBoost = 0.5f;
        [SerializeField] private float hitBoostDuration = 1.5f;

        [Header("Wall Sprites")]
        [SerializeField] private SpriteRenderer wallLeft;
        [SerializeField] private SpriteRenderer wallRight;

        [Header("Enable/Disable")]
        [SerializeField] private bool leftWallEnabled = true;
        [SerializeField] private bool rightWallEnabled = true;

        public float CurrentHalfWidth { get; private set; }
        public bool LeftWallEnabled => leftWallEnabled;
        public bool RightWallEnabled => rightWallEnabled;

        private bool isCompressing;
        private float hitBoostTimer;
        private float currentCompressSpeed;

        private void Awake()
        {
            CurrentHalfWidth = initialHalfWidth;
            currentCompressSpeed = compressSpeed;

            if (wallLeft != null)
                wallLeft.gameObject.SetActive(leftWallEnabled);
            if (wallRight != null)
                wallRight.gameObject.SetActive(rightWallEnabled);
        }

        private void Update()
        {
            if (!isCompressing) return;

            if (hitBoostTimer > 0f)
            {
                hitBoostTimer -= Time.deltaTime;
                if (hitBoostTimer <= 0f)
                    currentCompressSpeed = compressSpeed;
            }

            CurrentHalfWidth = Mathf.Max(minHalfWidth, CurrentHalfWidth - currentCompressSpeed * Time.deltaTime);
            UpdateWallPositions();
        }

        // --- WorkspaceEventRouter 调用的 public 入口 ---

        public void OnPhase1()
        {
            isCompressing = true;
            CurrentHalfWidth = initialHalfWidth;
        }

        public void OnPhase2()
        {
            isCompressing = false;
            hitBoostTimer = 0f;
            currentCompressSpeed = compressSpeed;
        }

        public void OnPlayerHit()
        {
            if (!isCompressing) return;
            currentCompressSpeed = compressSpeed + hitSpeedBoost;
            hitBoostTimer = hitBoostDuration;
        }

        // ---

        public void SetLeftWallEnabled(bool enabled)
        {
            leftWallEnabled = enabled;
            if (wallLeft != null)
                wallLeft.gameObject.SetActive(enabled);
        }

        public void SetRightWallEnabled(bool enabled)
        {
            rightWallEnabled = enabled;
            if (wallRight != null)
                wallRight.gameObject.SetActive(enabled);
        }

        private void UpdateWallPositions()
        {
            if (wallLeft != null && leftWallEnabled)
                wallLeft.transform.localPosition = new Vector3(-CurrentHalfWidth, 0f, 0f);
            if (wallRight != null && rightWallEnabled)
                wallRight.transform.localPosition = new Vector3(CurrentHalfWidth, 0f, 0f);
        }

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            Gizmos.color = new Color(1f, 0f, 0f, 0.3f);
            Gizmos.DrawWireCube(Vector3.zero, new Vector3(CurrentHalfWidth * 2f, 10f, 0));
        }
#endif
    }
}
