using UnityEngine;

namespace GameCreate3
{
    [RequireComponent(typeof(Rigidbody2D))]
    public sealed class DreamGoalAxisLock : MonoBehaviour
    {
        private Rigidbody2D body;
        private float fixedY;

        private void Awake()
        {
            body = GetComponent<Rigidbody2D>();
            fixedY = transform.position.y;
        }

        private void LateUpdate()
        {
            var position = transform.position;
            if (!Mathf.Approximately(position.y, fixedY))
            {
                transform.position = new Vector3(position.x, fixedY, position.z);
            }
        }

        private void FixedUpdate()
        {
            if (body == null || body.bodyType == RigidbodyType2D.Static)
            {
                return;
            }

            var velocity = body.velocity;
            velocity.y = 0f;
            body.velocity = velocity;
        }
    }
}
