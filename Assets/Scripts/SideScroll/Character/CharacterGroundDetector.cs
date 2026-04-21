using UnityEngine;

namespace GameCreate3
{
    public sealed class CharacterGroundDetector : MonoBehaviour
    {
        [SerializeField] private Transform groundCheckPoint;
        [SerializeField] private float groundCheckRadius = 0.18f;
        [SerializeField] private LayerMask groundMask = ~0;

        public bool IsGrounded { get; private set; }

        public void SetGroundMask(LayerMask mask)
        {
            groundMask = mask;
        }

        public bool Sample()
        {
            var checkPosition = groundCheckPoint != null ? groundCheckPoint.position : transform.position;
            IsGrounded = Physics2D.OverlapCircle(checkPosition, groundCheckRadius, groundMask) != null;
            return IsGrounded;
        }

        private void OnDrawGizmosSelected()
        {
            var checkPosition = groundCheckPoint != null ? groundCheckPoint.position : transform.position;
            Gizmos.color = IsGrounded ? Color.green : Color.red;
            Gizmos.DrawWireSphere(checkPosition, groundCheckRadius);
        }
    }
}
