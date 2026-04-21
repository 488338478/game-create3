using UnityEngine;

namespace GameCreate3
{
    public sealed class CharacterMovementMotor : MonoBehaviour
    {
        [SerializeField] private Rigidbody2D targetBody;
        [SerializeField] private CharacterMoveConfig config;

        private void Awake()
        {
            if (targetBody == null)
            {
                targetBody = GetComponent<Rigidbody2D>();
            }
        }

        public void SetConfig(CharacterMoveConfig value)
        {
            config = value;
        }

        public void Apply(float moveInput, bool isGrounded)
        {
            if (targetBody == null || config == null)
            {
                return;
            }

            var velocity = targetBody.velocity;
            var targetSpeed = moveInput * config.maxSpeed;
            var accel = Mathf.Abs(targetSpeed) > 0.01f ? config.acceleration : config.deceleration;
            if (!isGrounded)
            {
                accel *= Mathf.Max(0f, config.airControlMultiplier);
            }

            velocity.x = Mathf.MoveTowards(velocity.x, targetSpeed, accel * Time.fixedDeltaTime);
            targetBody.velocity = velocity;
        }
    }
}
