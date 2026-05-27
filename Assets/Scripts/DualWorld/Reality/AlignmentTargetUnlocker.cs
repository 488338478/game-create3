using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace GameCreate3.DualWorld
{
    /// <summary>
    /// 挂在每个 Target_X 上，被 RealityAlignmentTask 唤起时激活并播解锁动效。
    /// 没挂 Animator 时走默认协程：scale 0→1 + Image alpha 0→1，约 popDurationSec 秒。
    /// 挂了 Animator/PlayableDirector 就让外部资产自己管动效，本组件只负责激活并触发 OnUnlocked。
    /// </summary>
    public sealed class AlignmentTargetUnlocker : MonoBehaviour
    {
        [SerializeField] private float popDurationSec = 0.25f;
        [SerializeField] private Animator animator;
        [SerializeField] private string animatorTriggerOnPlay = "Unlock";
        [SerializeField] private UnityEvent onUnlocked;

        private bool playing;

        public bool HasPlayed { get; private set; }

        public void Play()
        {
            if (playing) return;
            playing = true;

            gameObject.SetActive(true);

            if (animator != null && animator.isActiveAndEnabled)
            {
                if (!string.IsNullOrEmpty(animatorTriggerOnPlay))
                {
                    animator.SetTrigger(animatorTriggerOnPlay);
                }
                FinishPlay();
                return;
            }

            StartCoroutine(PopRoutine());
        }

        public void ResetUnlocker()
        {
            StopAllCoroutines();
            playing = false;
            HasPlayed = false;
            gameObject.SetActive(false);
        }

        private IEnumerator PopRoutine()
        {
            var rect = transform as RectTransform;
            var graphics = GetComponentsInChildren<Graphic>(true);
            var startScale = Vector3.zero;
            var endScale = Vector3.one;
            var t = 0f;
            var duration = Mathf.Max(0.01f, popDurationSec);

            if (rect != null) rect.localScale = startScale;
            ApplyAlpha(graphics, 0f);

            while (t < duration)
            {
                t += Time.unscaledDeltaTime;
                var k = Mathf.Clamp01(t / duration);
                var ease = 1f - Mathf.Pow(1f - k, 3f);
                if (rect != null) rect.localScale = Vector3.LerpUnclamped(startScale, endScale, ease);
                ApplyAlpha(graphics, ease);
                yield return null;
            }

            if (rect != null) rect.localScale = endScale;
            ApplyAlpha(graphics, 1f);

            FinishPlay();
        }

        private static void ApplyAlpha(Graphic[] graphics, float alpha)
        {
            if (graphics == null) return;
            for (var i = 0; i < graphics.Length; i++)
            {
                var g = graphics[i];
                if (g == null) continue;
                var c = g.color;
                c.a = alpha;
                g.color = c;
            }
        }

        private void FinishPlay()
        {
            playing = false;
            HasPlayed = true;
            onUnlocked?.Invoke();
        }
    }
}
