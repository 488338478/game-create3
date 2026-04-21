using UnityEngine;

namespace GameCreate3
{
    [CreateAssetMenu(menuName = "GameCreate3/SideScroll/Character Move Config", fileName = "CharacterMoveConfig")]
    public sealed class CharacterMoveConfig : ScriptableObject
    {
        [Min(0f)] public float maxSpeed = 6f;
        [Min(0f)] public float acceleration = 24f;
        [Min(0f)] public float deceleration = 30f;
        [Min(0f)] public float airControlMultiplier = 0.75f;
    }
}
