using UnityEngine;

namespace GameCreate3
{
    public sealed class SideScrollCameraFollow : MonoBehaviour
    {
        [SerializeField] private Transform target;
        [SerializeField] private Vector3 offset = new Vector3(0f, 1f, -10f);
        [SerializeField] private float smoothTime = 0.2f;

        [Header("Bounds")]
        [SerializeField] private bool useBounds;
        [SerializeField] private float minX = -100f;
        [SerializeField] private float maxX = 100f;
        [SerializeField] private float minY = -10f;
        [SerializeField] private float maxY = 30f;

        private Vector3 velocity;

        private void LateUpdate()
        {
            if (target == null)
            {
                return;
            }

            var desired = target.position + offset;
            if (useBounds)
            {
                desired.x = Mathf.Clamp(desired.x, minX, maxX);
                desired.y = Mathf.Clamp(desired.y, minY, maxY);
            }

            transform.position = Vector3.SmoothDamp(transform.position, desired, ref velocity, smoothTime);
        }
    }
}
