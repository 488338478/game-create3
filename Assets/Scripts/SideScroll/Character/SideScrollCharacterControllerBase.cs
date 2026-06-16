using System;
using UnityEngine;

namespace GameCreate3
{
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(Collider2D))]
    [RequireComponent(typeof(CharacterInputProxy))]
    [RequireComponent(typeof(CharacterGroundDetector))]
    [RequireComponent(typeof(CharacterMovementMotor))]
    [RequireComponent(typeof(CharacterJumpMotor))]
    [RequireComponent(typeof(SideScrollInteractionDetector))]
    public sealed class SideScrollCharacterControllerBase : MonoBehaviour
    {
        private readonly DisabledInputSource disabledInputSource = new DisabledInputSource();

        [SerializeField] private CharacterInputProxy inputProxy;
        [SerializeField] private CharacterGroundDetector groundDetector;
        [SerializeField] private CharacterMovementMotor movementMotor;
        [SerializeField] private CharacterJumpMotor jumpMotor;
        [SerializeField] private SideScrollInteractionDetector interactionDetector;

        [Header("Input Bindings")]
        [Tooltip("交互键的 Input System binding 路径。留空则回落到默认（E / Enter / 手柄西键）。\n常用示例：<Keyboard>/e、<Keyboard>/space、<Gamepad>/buttonSouth")]
        [SerializeField] private string[] interactBindings =
        {
            "<Keyboard>/e",
            "<Keyboard>/enter",
            "<Gamepad>/buttonWest",
        };

        private ICharacterInputSource activeInputSource;
        private PlayerInputSource playerInputSource;
        private bool inputEnabled = true;
        // 落地声基线。初值 true：出生时若已落地不会误触发；出生在空中则落地会正常播。
        private bool wasGroundedForSfx = true;

        public bool IsGrounded { get; private set; }
        public bool InputEnabled => inputEnabled;

        private void Awake()
        {
            inputProxy = inputProxy != null ? inputProxy : GetComponent<CharacterInputProxy>();
            groundDetector = groundDetector != null ? groundDetector : GetComponent<CharacterGroundDetector>();
            movementMotor = movementMotor != null ? movementMotor : GetComponent<CharacterMovementMotor>();
            jumpMotor = jumpMotor != null ? jumpMotor : GetComponent<CharacterJumpMotor>();
            interactionDetector = interactionDetector != null ? interactionDetector : GetComponent<SideScrollInteractionDetector>();

            playerInputSource = new PlayerInputSource(interactBindings);
            activeInputSource = playerInputSource;
            inputProxy.SetInputSource(activeInputSource);
        }

        private void Update()
        {
            inputProxy.Tick();
            // Interaction 走 Update（点对点逻辑，不依赖物理）—— 消费后立刻清锁存。
            if (inputProxy.InteractPressed)
            {
                interactionDetector.ProcessInteraction(gameObject, true);
                inputProxy.ConsumeInteractPressed();
            }
            else
            {
                interactionDetector.ProcessInteraction(gameObject, false);
            }
        }

        private void FixedUpdate()
        {
            IsGrounded = groundDetector.Sample();
            movementMotor.Apply(inputProxy.MoveX, IsGrounded);
            jumpMotor.Tick(IsGrounded, inputProxy.JumpPressed, inputProxy.JumpHeld);
            // 跳跃锁存被消费后立刻清掉，避免 jumpBuffer 自然衰减期间被多次重新填满。
            inputProxy.ConsumeJumpPressed();

            // 落地声：和地面检测同一物理帧触发，零额外帧延迟（比放在 Update 里早一帧）。
            // inputEnabled 作闸——死亡/重生接管期间（SetInputEnabled(false)）不播放，
            // 避免泡泡搬运、瞬移过程中误触发落地声。
            if (inputEnabled && !wasGroundedForSfx && IsGrounded)
            {
                GameCreate3.Core.GameAudioService.Instance?.PlaySFX("SFX_Land");
            }
            wasGroundedForSfx = IsGrounded;
        }

        private void OnDestroy()
        {
            if (playerInputSource != null)
            {
                playerInputSource.Dispose();
                playerInputSource = null;
            }
        }

        public void SetInputEnabled(bool enabled)
        {
            inputEnabled = enabled;
            // 重新接管输入时（如重生结束），把落地基线对齐到当前着地状态，
            // 避免重生落点这一帧被判成"刚落地"而误播落地声。
            if (enabled)
            {
                wasGroundedForSfx = groundDetector != null && groundDetector.IsGrounded;
            }
            // 直接换 proxy 的源，不要走 SetInputSource —— 那个会把 disabled 写进 activeInputSource，
            // 导致下次 enable 时 activeInputSource ?? playerInputSource 仍取到 disabled，玩家永久锁定。
            inputProxy.SetInputSource(enabled ? (activeInputSource ?? playerInputSource) : disabledInputSource);
        }

        public void SetInputSource(ICharacterInputSource source)
        {
            if (source != null)
            {
                activeInputSource = source;
            }
            else if (playerInputSource != null)
            {
                activeInputSource = playerInputSource;
            }
            else
            {
                activeInputSource = disabledInputSource;
            }

            inputProxy.SetInputSource(inputEnabled ? activeInputSource : disabledInputSource);
        }

        public void ApplyConfigs(CharacterMoveConfig moveConfig, CharacterJumpConfig jumpConfig, LayerMask groundMask)
        {
            movementMotor.SetConfig(moveConfig);
            jumpMotor.SetConfig(jumpConfig);
            groundDetector.SetGroundMask(groundMask);
        }
    }
}
