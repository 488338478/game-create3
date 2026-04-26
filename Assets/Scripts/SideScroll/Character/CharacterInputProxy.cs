using UnityEngine;

namespace GameCreate3
{
    public sealed class CharacterInputProxy : MonoBehaviour
    {
        private ICharacterInputSource inputSource;

        public float MoveX { get; private set; }
        public bool JumpPressed { get; private set; }
        public bool JumpHeld { get; private set; }
        public bool InteractPressed { get; private set; }

        public void SetInputSource(ICharacterInputSource source)
        {
            inputSource = source;
            ResetState();
        }

        public void Tick()
        {
            if (inputSource == null)
            {
                ResetState();
                return;
            }

            inputSource.Tick();
            MoveX = inputSource.MoveX;
            JumpPressed = inputSource.JumpPressed;
            JumpHeld = inputSource.JumpHeld;
            InteractPressed = inputSource.InteractPressed;
        }

        private void ResetState()
        {
            MoveX = 0f;
            JumpPressed = false;
            JumpHeld = false;
            InteractPressed = false;
        }
    }
}
