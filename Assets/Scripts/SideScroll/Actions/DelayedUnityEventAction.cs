using System.Collections;
using UnityEngine;
using UnityEngine.Events;

namespace GameCreate3
{
    [DisallowMultipleComponent]
    public sealed class DelayedUnityEventAction : MonoBehaviour
    {
        [SerializeField, Min(0f), Tooltip("调用 InvokeAfterDelay 后等待多少秒再触发 onComplete。")]
        private float delaySeconds = 1f;

        [SerializeField, Tooltip("延迟结束后触发的事件。")]
        private UnityEvent onComplete;

        private Coroutine running;

        public void InvokeAfterDelay()
        {
            if (running != null)
            {
                StopCoroutine(running);
            }
            running = StartCoroutine(Run());
        }

        public void Cancel()
        {
            if (running == null) return;
            StopCoroutine(running);
            running = null;
        }

        private IEnumerator Run()
        {
            yield return new WaitForSeconds(delaySeconds);
            running = null;
            onComplete?.Invoke();
        }
    }
}
