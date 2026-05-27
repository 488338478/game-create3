using UnityEngine;

namespace GameCreate3
{
    /// <summary>
    /// 通过 UnityEvent 触发一段 Animator 行为，两种模式：
    ///   - PlayState: 直接 Play 一个 state（适合一次性动画）
    ///   - SetTrigger: 给 Animator 设一个 trigger 参数（适合 state machine 内部转移）
    /// 留空 targetAnimator 时自动在自身 / 父链 / 子链上找 Animator。
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class AnimatorPlayAction : MonoBehaviour
    {
        public enum Mode { PlayState, SetTrigger }

        [SerializeField, Tooltip("留空 = 自动在自身/父/子链找 Animator。")]
        private Animator targetAnimator;

        [SerializeField] private Mode mode = Mode.PlayState;

        [SerializeField, Tooltip("PlayState 时是 state 的名字；SetTrigger 时是 trigger 参数名。")]
        private string stateOrTriggerName = "";

        [SerializeField, Tooltip("PlayState 使用的 layer，-1 = 默认。")]
        private int layer = -1;

        [SerializeField, Tooltip("PlayState 时是否从 0 时刻开始播放。")]
        private bool restartFromStart = true;

        private void Awake()
        {
            if (targetAnimator == null)
            {
                targetAnimator = GetComponent<Animator>()
                    ?? GetComponentInParent<Animator>()
                    ?? GetComponentInChildren<Animator>();
            }
        }

        public void Play() => Play(stateOrTriggerName);

        public void Play(string overrideName)
        {
            if (targetAnimator == null)
            {
                Debug.LogWarning($"[AnimatorPlayAction] '{name}' 找不到 Animator。", this);
                return;
            }
            var n = string.IsNullOrEmpty(overrideName) ? stateOrTriggerName : overrideName;
            if (string.IsNullOrEmpty(n))
            {
                Debug.LogWarning($"[AnimatorPlayAction] '{name}' state/trigger 名为空。", this);
                return;
            }

            if (mode == Mode.PlayState)
            {
                targetAnimator.Play(n, layer, restartFromStart ? 0f : float.NegativeInfinity);
            }
            else
            {
                targetAnimator.SetTrigger(n);
            }
        }
    }
}
