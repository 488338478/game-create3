using UnityEngine;

namespace GameCreate3
{
    [RequireComponent(typeof(Rigidbody2D))]
    public sealed class SideScrollerPlayerController : MonoBehaviour
    {
        [Header("Movement")]
        [SerializeField] private float moveSpeed = 5f;
        [SerializeField] private float jumpForce = 10f;

        [Header("Ground Check")]
        [SerializeField] private Transform groundCheckPoint;
        [SerializeField] private float groundCheckRadius = 0.2f;
        [SerializeField] private LayerMask groundMask = ~0;

        private Rigidbody2D rb;
        private float moveInput;
        private bool jumpQueued;
        private bool inputLocked;

        public bool IsGrounded { get; private set; }
        public bool InputLocked => inputLocked;

        private void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
        }

        private void Update()
        {
            if (inputLocked)
            {
                moveInput = 0f;
                jumpQueued = false;
                return;
            }

            moveInput = Input.GetAxisRaw("Horizontal");

            if (Input.GetButtonDown("Jump"))
            {
                jumpQueued = true;
            }
        }

        private void FixedUpdate()
        {
            UpdateGrounded();

            var velocity = rb.velocity;
            velocity.x = moveInput * moveSpeed;

            if (jumpQueued && IsGrounded)
            {
                velocity.y = jumpForce;
            }

            rb.velocity = velocity;
            jumpQueued = false;
        }

        public void SetInputLocked(bool locked)
        {
            inputLocked = locked;
            if (locked)
            {
                rb.velocity = new Vector2(0f, rb.velocity.y);
            }
        }

        private void UpdateGrounded()
        {
            var checkPoint = groundCheckPoint != null ? groundCheckPoint.position : transform.position;
            IsGrounded = Physics2D.OverlapCircle(checkPoint, groundCheckRadius, groundMask) != null;
        }

        private void OnDrawGizmosSelected()
        {
            var checkPoint = groundCheckPoint != null ? groundCheckPoint.position : transform.position;
            Gizmos.color = IsGrounded ? Color.green : Color.red;
            Gizmos.DrawWireSphere(checkPoint, groundCheckRadius);
        }
    }
}
