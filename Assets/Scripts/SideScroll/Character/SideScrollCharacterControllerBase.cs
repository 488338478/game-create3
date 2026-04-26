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

        private ICharacterInputSource activeInputSource;
        private PlayerInputSource playerInputSource;
        private bool inputEnabled = true;

        public bool IsGrounded { get; private set; }

        private void Awake()
        {
            inputProxy = inputProxy != null ? inputProxy : GetComponent<CharacterInputProxy>();
            groundDetector = groundDetector != null ? groundDetector : GetComponent<CharacterGroundDetector>();
            movementMotor = movementMotor != null ? movementMotor : GetComponent<CharacterMovementMotor>();
            jumpMotor = jumpMotor != null ? jumpMotor : GetComponent<CharacterJumpMotor>();
            interactionDetector = interactionDetector != null ? interactionDetector : GetComponent<SideScrollInteractionDetector>();

            playerInputSource = new PlayerInputSource();
            activeInputSource = playerInputSource;
            inputProxy.SetInputSource(activeInputSource);
        }

        private void Update()
        {
            inputProxy.Tick();
            interactionDetector.ProcessInteraction(gameObject, inputProxy.InteractPressed);
        }

        private void FixedUpdate()
        {
            IsGrounded = groundDetector.Sample();
            movementMotor.Apply(inputProxy.MoveX, IsGrounded);
            jumpMotor.Tick(IsGrounded, inputProxy.JumpPressed, inputProxy.JumpHeld);
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
            SetInputSource(enabled ? activeInputSource ?? playerInputSource : disabledInputSource);
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
