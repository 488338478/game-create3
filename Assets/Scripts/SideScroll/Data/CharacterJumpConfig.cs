using UnityEngine;

namespace GameCreate3
{
    [CreateAssetMenu(menuName = "GameCreate3/SideScroll/Character Jump Config", fileName = "CharacterJumpConfig")]
    public sealed class CharacterJumpConfig : ScriptableObject
    {
        public float jumpForce = 9f;
        public float fallGravityMultiplier = 2.2f;
        public float coyoteTime = 0.1f;
        public float jumpBuffer = 0.12f;
        public float maxFallSpeed = 16f;
    }
}
