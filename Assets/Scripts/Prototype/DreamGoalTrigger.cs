using System;
using UnityEngine;

namespace GameCreate3
{
    public sealed class DreamGoalTrigger : MonoBehaviour
    {
        private bool completed;
        private Transform destination;
        private Transform movingGoal;
        private Rigidbody2D goalBody;
        [SerializeField] private float completionDistance = 0.08f;

        public event Action Completed;

        public void Initialize(Transform goalTransform, Transform destinationTransform)
        {
            movingGoal = goalTransform;
            destination = destinationTransform;
            goalBody = goalTransform != null ? goalTransform.GetComponent<Rigidbody2D>() : null;
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
    }
}
