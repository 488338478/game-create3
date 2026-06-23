using System;
using System.Collections.Generic;
using UnityEngine.InputSystem;

namespace GameCreate3
{
    public sealed class PlayerInputSource : ICharacterInputSource, IDisposable
    {
        public static readonly string[] DefaultInteractBindings =
        {
            "<Keyboard>/e",
            "<Keyboard>/enter",
            "<Gamepad>/buttonWest",
        };

        private readonly InputAction moveAction;
        private readonly InputAction jumpAction;
        private readonly InputAction interactAction;

        public PlayerInputSource() : this(null) { }

        public PlayerInputSource(IReadOnlyList<string> interactBindings)
        {
            moveAction = new InputAction("Move", InputActionType.Value);
            moveAction.AddCompositeBinding("1DAxis")
                .With("Negative", "<Keyboard>/a")
                .With("Positive", "<Keyboard>/d");
            moveAction.AddCompositeBinding("1DAxis")
                .With("Negative", "<Keyboard>/leftArrow")
                .With("Positive", "<Keyboard>/rightArrow");
            moveAction.AddBinding("<Gamepad>/leftStick/x");

            jumpAction = new InputAction("Jump", InputActionType.Button);
            jumpAction.AddBinding("<Keyboard>/space");
            jumpAction.AddBinding("<Keyboard>/w");
            jumpAction.AddBinding("<Keyboard>/upArrow");
            jumpAction.AddBinding("<Gamepad>/buttonSouth");

            interactAction = new InputAction("Interact", InputActionType.Button);
            // 没传或全空就回落到默认（E / Enter / 手柄西键），避免误配置后玩家完全无法交互。
            var bindings = (interactBindings != null && interactBindings.Count > 0)
                ? interactBindings
                : DefaultInteractBindings;
            foreach (var binding in bindings)
            {
                if (!string.IsNullOrWhiteSpace(binding))
                {
                    interactAction.AddBinding(binding);
                }
            }

            moveAction.Enable();
            jumpAction.Enable();
            interactAction.Enable();
        }

        public float MoveX { get; private set; }
        public bool JumpPressed { get; private set; }
        public bool InteractPressed { get; private set; }

        public void Tick()
        {
            MoveX = moveAction.ReadValue<float>();
            JumpPressed = jumpAction.WasPressedThisFrame();
            InteractPressed = interactAction.WasPressedThisFrame();
        }

        public void Dispose()
        {
            moveAction.Dispose();
            jumpAction.Dispose();
            interactAction.Dispose();
        }
    }
}
