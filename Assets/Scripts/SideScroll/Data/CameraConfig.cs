using UnityEngine;

namespace GameCreate3
{
    [CreateAssetMenu(menuName = "GameCreate3/SideScroll/Camera Config", fileName = "CameraConfig")]
    public sealed class CameraConfig : ScriptableObject
    {
        public Vector3 followOffset = new Vector3(0f, 1.2f, -10f);
        public Vector2 damping = new Vector2(0.2f, 0.2f);
        public bool useConfiner = true;
        public float orthographicSize = 5f;
    }
}
