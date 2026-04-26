using UnityEngine;

namespace GameCreate3
{
    public sealed class CharacterJumpMotor : MonoBehaviour
    {
        [SerializeField] private Rigidbody2D targetBody;
        [SerializeField] private CharacterJumpConfig config;

        private float coyoteCounter;
        private float jumpBufferCounter;

        private void Awake()
        {
            if (targetBody == null)
            {
                targetBody = GetComponent<Rigidbody2D>();
            }
        }

        public void SetConfig(CharacterJumpConfig value)
        {
            config = value;
        }

        public void Tick(bool isGrounded, bool jumpPressed, bool jumpHeld)
        {
            if (targetBody == null || config == null)
            {
                return;
            }

            coyoteCounter = isGrounded ? config.coyoteTime : Mathf.Max(0f, coyoteCounter - Time.fixedDeltaTime);
            jumpBufferCounter = jumpPressed ? config.jumpBuffer : Mathf.Max(0f, jumpBufferCounter - Time.fixedDeltaTime);

            var velocity = targetBody.velocity;
            if (jumpBufferCounter > 0f && coyoteCounter > 0f)
            {
                velocity.y = config.jumpForce;
                jumpBufferCounter = 0f;
                coyoteCounter = 0f;
            }

            if (!jumpHeld && velocity.y > 0f)
            {
                velocity.y *= 0.92f;
            }

            if (velocity.y < 0f)
            {
                velocity.y = Mathf.Max(velocity.y - (config.fallGravityMultiplier - 1f) * Physics2D.gravity.y * Time.fixedDeltaTime, -config.maxFallSpeed);
            }

            targetBody.velocity = velocity;
        }
    }
}
