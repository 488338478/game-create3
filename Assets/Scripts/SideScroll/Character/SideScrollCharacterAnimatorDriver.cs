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
        private float    _stepTimer;        // 脚步声节奏计时
        private bool     _frozen;           // 被泡泡等外部接管时锁死，不再驱动 Animator
        private const float StepInterval = 0.32f;

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

        /// <summary>
        /// 外部接管时冻结/解冻动画驱动。
        /// 冻结期间 Update 完全不写 Animator（Speed/Jump 都不动），
        /// 角色动作保持在接管方设置的状态（如 Death）或自然回到 Idle，避免在泡泡里乱切。
        /// 由 <see cref="DeathRespawnTriggerZone"/>（重生泡泡）与 <see cref="SineMover"/>（终点泡泡）调用。
        /// </summary>
        public void SetAnimationFrozen(bool frozen)
        {
            if (_frozen == frozen) return;
            _frozen = frozen;

            if (_animator == null) return;

            if (frozen)
            {
                // 锁死前清零移动参数、清掉残留的 Jump 触发，给接管方一个干净的基线。
                _animator.SetFloat(_speedId, 0f);
                _animator.ResetTrigger(_jumpId);
                _stepTimer = 0f;
            }
            else
            {
                // 解冻时重置落地/腾空基线，避免接管结束瞬间误触发 Jump 或落地音。
                var grounded = groundDetector != null && groundDetector.IsGrounded;
                _wasGrounded = grounded;
                _airTicks = grounded ? 0 : jumpGraceTicks + 1;
                _stepTimer = 0f;
            }
        }

        private void Update()
        {
            if (_frozen) return;
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

            // ── 脚步声（着地且水平移动时按节奏播）+ 落地声 ─────────────
            if (!_wasGrounded && isGrounded)
            {
                GameCreate3.Core.GameAudioService.Instance?.PlaySFX("SFX_Land");
            }

            if (isGrounded && hSpeed >= moveThreshold)
            {
                _stepTimer -= Time.deltaTime;
                if (_stepTimer <= 0f)
                {
                    GameCreate3.Core.GameAudioService.Instance?.PlaySFX("SFX_Footstep_Step");
                    _stepTimer = StepInterval;
                }
            }
            else
            {
                _stepTimer = 0f;
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
