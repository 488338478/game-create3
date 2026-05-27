using UnityEngine;

namespace GameCreate3
{
    /// <summary>
    /// 将 SideScrollCharacterControllerBase 的物理状态映射到 Animator 参数。
    /// 支持 bear.controller 的 Speed(Float) + Jump(Trigger)。
    /// 挂在与角色控制器相同的 GameObject 上，或通过 Inspector 指定目标。
    /// </summary>
    [RequireComponent(typeof(Animator))]
    public sealed class SideScrollCharacterAnimatorDriver : MonoBehaviour
    {
        [Header("依赖（留空自动查找）")]
        [SerializeField] private Rigidbody2D body;
        [SerializeField] private CharacterGroundDetector groundDetector;

        [Header("Animator 参数名")]
        [SerializeField] private string speedParam  = "Speed";
        [SerializeField] private string jumpParam   = "Jump";

        [Header("Speed 映射")]
        [Tooltip("速度绝对值超过此值才算移动，避免抖动")]
        [SerializeField] private float moveThreshold = 0.05f;
        [Tooltip("将水平速度除以此值得到 Speed 参数（1 = 走；≥ runSpeedThreshold = 跑）")]
        [SerializeField] private float speedScale = 1f;

        [Header("Jump 检测")]
        [Tooltip("从落地到起跳的最大检测帧数（防漏触发）")]
        [SerializeField] private int jumpGraceTicks = 3;

        [Header("转向（调 localScale.x）")]
        [Tooltip("要翻转的对象，留空则用本物体 transform")]
        [SerializeField] private Transform facingTarget;
        [Tooltip("精灵默认朝向：true=美术原图朝右，false=朝左")]
        [SerializeField] private bool spriteFacesRight = true;

        // ── 私有状态 ──────────────────────────────────────────────
        private Animator _animator;
        private int      _speedId;
        private int      _jumpId;
        private bool     _wasGrounded;
        private int      _airTicks;
        private int      _facingSign = 1;   // 1=右, -1=左

        private void Awake()
        {
            _animator = GetComponent<Animator>();
            _speedId  = Animator.StringToHash(speedParam);
            _jumpId   = Animator.StringToHash(jumpParam);

            if (body == null)
                body = GetComponentInParent<Rigidbody2D>();

            if (groundDetector == null)
                groundDetector = GetComponentInParent<CharacterGroundDetector>();

            if (facingTarget == null)
                facingTarget = transform;
        }

        private void Update()
        {
            if (body == null) return;

            var vx = body.velocity.x;

            // ── Speed ─────────────────────────────────────────────
            var hSpeed = Mathf.Abs(vx);
            var scaledSpeed = hSpeed < moveThreshold ? 0f : hSpeed / Mathf.Max(0.001f, speedScale);
            _animator.SetFloat(_speedId, scaledSpeed);

            // ── 转向（仅在确实移动时翻转，避免抖动）──────────────────
            if (hSpeed >= moveThreshold)
            {
                var moveSign = vx > 0f ? 1 : -1;
                if (moveSign != _facingSign)
                {
                    _facingSign = moveSign;
                    ApplyFacing();
                }
            }

            // ── Jump（落地→腾空 = 跳跃触发）────────────────────────
            var isGrounded = groundDetector != null ? groundDetector.IsGrounded : false;

            if (_wasGrounded && !isGrounded)
            {
                // 刚离地
                _airTicks = 0;
            }

            if (!isGrounded)
            {
                _airTicks++;
                if (_airTicks == 1)                     // 第一帧离地就触发
                    _animator.SetTrigger(_jumpId);
            }

            _wasGrounded = isGrounded;
        }

        private void ApplyFacing()
        {
            if (facingTarget == null) return;

            // 朝右时 sign=+1；若美术原图朝左则取反
            var wantRight = _facingSign > 0;
            var positiveScale = spriteFacesRight ? wantRight : !wantRight;

            var s = facingTarget.localScale;
            s.x = Mathf.Abs(s.x) * (positiveScale ? 1f : -1f);
            facingTarget.localScale = s;
        }
    }
}
