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
            JumpHeld = inputSource.JumpHeld;
            // 按下事件采用"或锁存"：Update 抓到 true 就置 true，由 FixedUpdate 消费后清零。
            // 否则在 Update 频率 > FixedUpdate 频率时，WasPressedThisFrame 这一帧捕获的值
            // 会被下一次 Tick 立刻刷成 false，跳跃 / 交互按键被丢。
            if (inputSource.JumpPressed) JumpPressed = true;
            if (inputSource.InteractPressed) InteractPressed = true;
        }

        public void ConsumeJumpPressed() => JumpPressed = false;
        public void ConsumeInteractPressed() => InteractPressed = false;

        private void ResetState()
        {
            MoveX = 0f;
            JumpPressed = false;
            JumpHeld = false;
            InteractPressed = false;
        }
    }
}
