using System;
using UnityEngine.InputSystem;

namespace GameCreate3
{
    public sealed class PlayerInputSource : ICharacterInputSource, IDisposable
    {
        private readonly InputAction moveAction;
        private readonly InputAction jumpAction;
        private readonly InputAction interactAction;

        public PlayerInputSource()
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
            interactAction.AddBinding("<Keyboard>/e");
            interactAction.AddBinding("<Keyboard>/enter");
            interactAction.AddBinding("<Gamepad>/buttonWest");

            moveAction.Enable();
            jumpAction.Enable();
            interactAction.Enable();
        }

        public float MoveX { get; private set; }
        public bool JumpPressed { get; private set; }
        public bool JumpHeld { get; private set; }
        public bool InteractPressed { get; private set; }

        public void Tick()
        {
            MoveX = moveAction.ReadValue<float>();
            JumpPressed = jumpAction.WasPressedThisFrame();
            JumpHeld = jumpAction.IsPressed();
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
