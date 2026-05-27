using UnityEngine;

namespace GameCreate3
{
    public sealed class CharacterGroundDetector : MonoBehaviour
    {
        [SerializeField] private Transform groundCheckPoint;
        [SerializeField] private float groundCheckRadius = 0.18f;
        [SerializeField] private LayerMask groundMask = ~0;
        [SerializeField] private Collider2D targetCollider;
        [SerializeField] private bool alignCheckYToColliderBottom = true;

        public bool IsGrounded { get; private set; }

        private void Awake()
        {
            if (targetCollider == null)
            {
                targetCollider = ResolvePhysicsCollider();
            }
        }

        private Collider2D ResolvePhysicsCollider()
        {
            // 玩家身上可能同时挂着物理 collider（CapsuleCollider2D）和交互探测 trigger（BoxCollider2D, isTrigger=true）。
            // ground 判定必须取"非 trigger"的那个，否则 bounds.min.y 会被 trigger 形状影响。
            foreach (var c in GetComponents<Collider2D>())
            {
                if (c != null && !c.isTrigger) return c;
            }
            return GetComponent<Collider2D>();
        }

        public void SetGroundMask(LayerMask mask)
        {
            groundMask = mask;
        }

        public bool Sample()
        {
            var checkPosition = ResolveCheckPosition();
            IsGrounded = Physics2D.OverlapCircle(checkPosition, groundCheckRadius, groundMask) != null;
            return IsGrounded;
        }

        private void OnDrawGizmosSelected()
        {
            var checkPosition = ResolveCheckPosition();
            Gizmos.color = IsGrounded ? Color.green : Color.red;
            Gizmos.DrawWireSphere(checkPosition, groundCheckRadius);
        }

        private Vector2 ResolveCheckPosition()
        {
            if (targetCollider == null)
            {
                targetCollider = ResolvePhysicsCollider();
            }

            var checkPosition = groundCheckPoint != null ? (Vector2)groundCheckPoint.position : (Vector2)transform.position;
            if (alignCheckYToColliderBottom && targetCollider != null)
            {
                // Keep horizontal tuning from groundCheckPoint, but pin Y near collider feet
                // so player scaling does not push probe far away from the ground.
                var feetY = targetCollider.bounds.min.y + groundCheckRadius * 0.5f;
                checkPosition.y = feetY;
            }

            return checkPosition;
        }
    }
}
