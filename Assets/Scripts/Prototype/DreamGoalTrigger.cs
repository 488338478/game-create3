using System;
using UnityEngine;

namespace GameCreate3
{
    public sealed class DreamGoalTrigger : MonoBehaviour
    {
        private bool completed;
        [SerializeField] private Transform destination;
        [SerializeField] private Transform movingGoal;
        private Rigidbody2D goalBody;
        [SerializeField] private float completionDistance = 0.08f;
        private Vector3 initialPosition;

        public event Action Completed;

        public void Initialize(Transform goalTransform, Transform destinationTransform)
        {
            movingGoal = goalTransform;
            destination = destinationTransform;
            goalBody = goalTransform != null ? goalTransform.GetComponent<Rigidbody2D>() : null;
        }

        private void Awake()
        {
            if (movingGoal == null)
            {
                movingGoal = transform;
            }

            goalBody = movingGoal != null ? movingGoal.GetComponent<Rigidbody2D>() : null;
            if (movingGoal != null)
            {
                initialPosition = movingGoal.position;
            }
        }

        private void Update()
        {
            if (completed || movingGoal == null || destination == null)
            {
                return;
            }

            var goalPosition = movingGoal.position;
            var destinationPosition = destination.position;

            if (Vector2.Distance(goalPosition, destinationPosition) > completionDistance)
            {
                return;
            }

            movingGoal.position = new Vector3(destinationPosition.x, destinationPosition.y, movingGoal.position.z);

            if (goalBody != null)
            {
                goalBody.velocity = Vector2.zero;
                goalBody.angularVelocity = 0f;
                goalBody.bodyType = RigidbodyType2D.Static;
            }

            completed = true;
            Completed?.Invoke();
        }

        public void ResetGoal()
        {
            completed = false;
            goalBody = movingGoal != null ? movingGoal.GetComponent<Rigidbody2D>() : null;
            if (movingGoal != null)
            {
                movingGoal.position = initialPosition;
            }
            if (goalBody != null && goalBody.bodyType == RigidbodyType2D.Static)
            {
                goalBody.bodyType = RigidbodyType2D.Dynamic;
                goalBody.gravityScale = 0f;
                goalBody.constraints = RigidbodyConstraints2D.FreezePositionY | RigidbodyConstraints2D.FreezeRotation;
                goalBody.velocity = Vector2.zero;
                goalBody.angularVelocity = 0f;
            }
        }
    }
}
