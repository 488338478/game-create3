using UnityEngine;

namespace GameCreate3.Level3
{
    public sealed class InvisibleWallController : MonoBehaviour
    {
        [Header("Walls (prefab instances with Collider2D)")]
        [SerializeField] private Transform wallLeft;
        [SerializeField] private Transform wallRight;

        [Header("Target Markers (empty GameObjects in scene)")]
        [SerializeField] private Transform leftTarget;
        [SerializeField] private Transform rightTarget;

        [Header("Enable/Disable")]
        [SerializeField] private bool leftWallEnabled = true;
        [SerializeField] private bool rightWallEnabled = true;

        [Header("Movement")]
        [SerializeField] private float totalDuration = 15f;
        [SerializeField] private float hitTimeReduction = 1f;
        [SerializeField, Range(0f, 1f)] private float parryPushBackRatio = 0.1f;
        [SerializeField] private float pushBackSpeed = 8f;

        private bool leftMoving;
        private bool rightMoving;
        private float leftSpeed;
        private float rightSpeed;
        private Vector3 leftStartPos;
        private Vector3 rightStartPos;

        private bool leftPushingBack;
        private bool rightPushingBack;
        private Vector3 leftPushTarget;
        private Vector3 rightPushTarget;

        private void Awake()
        {
            if (wallLeft != null)
            {
                leftStartPos = wallLeft.position;
                wallLeft.gameObject.SetActive(leftWallEnabled);
            }
            if (wallRight != null)
            {
                rightStartPos = wallRight.position;
                wallRight.gameObject.SetActive(rightWallEnabled);
            }
        }

        private void Update()
        {
            if (leftWallEnabled && wallLeft != null)
            {
                if (leftPushingBack)
                {
                    wallLeft.position = Vector3.MoveTowards(wallLeft.position, leftPushTarget, pushBackSpeed * Time.deltaTime);
                    if (wallLeft.position == leftPushTarget)
                        leftPushingBack = false;
                }
                else if (leftMoving && leftTarget != null)
                {
                    wallLeft.position = Vector3.MoveTowards(wallLeft.position, leftTarget.position, leftSpeed * Time.deltaTime);
                    if (wallLeft.position == leftTarget.position)
                        leftMoving = false;
                }
            }

            if (rightWallEnabled && wallRight != null)
            {
                if (rightPushingBack)
                {
                    wallRight.position = Vector3.MoveTowards(wallRight.position, rightPushTarget, pushBackSpeed * Time.deltaTime);
                    if (wallRight.position == rightPushTarget)
                        rightPushingBack = false;
                }
                else if (rightMoving && rightTarget != null)
                {
                    wallRight.position = Vector3.MoveTowards(wallRight.position, rightTarget.position, rightSpeed * Time.deltaTime);
                    if (wallRight.position == rightTarget.position)
                        rightMoving = false;
                }
            }
        }

        // --- Public API ---

        public void StartMoving()
        {
            if (leftWallEnabled && wallLeft != null && leftTarget != null)
            {
                float dist = Vector3.Distance(wallLeft.position, leftTarget.position);
                leftSpeed = dist / Mathf.Max(totalDuration, 0.01f);
                leftMoving = true;
            }
            if (rightWallEnabled && wallRight != null && rightTarget != null)
            {
                float dist = Vector3.Distance(wallRight.position, rightTarget.position);
                rightSpeed = dist / Mathf.Max(totalDuration, 0.01f);
                rightMoving = true;
            }
        }

        public void StopMoving()
        {
            leftMoving = false;
            rightMoving = false;
        }

        public void AccelerateOnHit()
        {
            totalDuration = Mathf.Max(totalDuration - hitTimeReduction, 0.5f);
            if (leftMoving && wallLeft != null && leftTarget != null)
            {
                float dist = Vector3.Distance(wallLeft.position, leftTarget.position);
                leftSpeed = dist / Mathf.Max(totalDuration, 0.01f);
            }
            if (rightMoving && wallRight != null && rightTarget != null)
            {
                float dist = Vector3.Distance(wallRight.position, rightTarget.position);
                rightSpeed = dist / Mathf.Max(totalDuration, 0.01f);
            }
        }

        public void PushBack()
        {
            if (leftWallEnabled && wallLeft != null && leftTarget != null)
            {
                float totalDist = Vector3.Distance(leftStartPos, leftTarget.position);
                float pushDist = totalDist * parryPushBackRatio;
                Vector3 dir = (leftStartPos - leftTarget.position).normalized;
                leftPushTarget = wallLeft.position + dir * pushDist;
                leftPushingBack = true;
            }
            if (rightWallEnabled && wallRight != null && rightTarget != null)
            {
                float totalDist = Vector3.Distance(rightStartPos, rightTarget.position);
                float pushDist = totalDist * parryPushBackRatio;
                Vector3 dir = (rightStartPos - rightTarget.position).normalized;
                rightPushTarget = wallRight.position + dir * pushDist;
                rightPushingBack = true;
            }
        }

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
    }
}
