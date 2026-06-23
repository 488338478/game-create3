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

        public float CurrentHalfWidth { get; private set; }

        private bool isCompressing;
        private float hitBoostTimer;
        private float currentCompressSpeed;

        private void Awake()
        {
            CurrentHalfWidth = initialHalfWidth;
            currentCompressSpeed = compressSpeed;
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

        private void UpdateWallPositions()
        {
            if (wallLeft != null)
                wallLeft.transform.localPosition = new Vector3(-CurrentHalfWidth, 0f, 0f);
            if (wallRight != null)
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
