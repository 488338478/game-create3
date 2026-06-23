using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GameCreate3.Level3
{
    public sealed class Level3XiaohongshuUI : MonoBehaviour
    {
        [Header("Main Panel")]
        [SerializeField] private GameObject panelRoot;

        [Header("Follower Display")]
        [SerializeField] private TextMeshProUGUI followerCountText;
        [SerializeField] private float countScrollDuration = 0.3f;
        [SerializeField] private float countJumpDuration = 0.15f;

        [Header("Comment Displays")]
        [SerializeField] private GameObject comment1;
        [SerializeField] private GameObject comment2;
        [SerializeField] private GameObject comment3;
        [SerializeField] private GameObject comment4;

        [Header("Red Point Indicator")]
        [SerializeField] private RectTransform redpoint;

        [Header("Status Text")]
        [SerializeField] private TextMeshProUGUI statusText;

        private FollowerCounter followerCounter;
        private Coroutine scrollRoutine;
        private int displayedCount;

        private void Awake()
        {
            followerCounter = GetComponentInParent<FollowerCounter>(true);
            if (panelRoot != null)
                panelRoot.SetActive(false);
        }

        // --- WorkspaceEventRouter 调用的 public 入口 ---

        public void OnPhase2()
        {
            if (panelRoot != null)
                panelRoot.SetActive(true);
            displayedCount = 0;
            if (followerCountText != null)
                followerCountText.text = "0";
        }

        public void OnFollowerChanged()
        {
            if (followerCounter == null || followerCountText == null) return;
            var target = followerCounter.CurrentFollowers;
            if (scrollRoutine != null)
                StopCoroutine(scrollRoutine);
            var isLargeJump = Mathf.Abs(target - displayedCount) > 5000;
            scrollRoutine = StartCoroutine(ScrollCountCoroutine(target, isLargeJump ? countJumpDuration : countScrollDuration));
        }

        public void OnComment1() => RevealComment(comment1);
        public void OnComment2() => RevealComment(comment2);
        public void OnComment3() => RevealComment(comment3);
        public void OnComment4() => RevealComment(comment4);

        private void RevealComment(GameObject comment)
        {
            if (comment == null) return;
            comment.SetActive(true);

            if (redpoint == null) return;
            redpoint.gameObject.SetActive(true);

            var commentRT = comment.GetComponent<RectTransform>();
            if (commentRT == null) return;

            var pos = commentRT.anchoredPosition;
            var size = commentRT.sizeDelta;
            redpoint.anchoredPosition = new Vector2(pos.x + size.x * 0.5f, pos.y + size.y * 0.5f);
        }

        public void OnSequenceCorrect() => SetStatusText("正确！继续前进~");
        public void OnSequenceWrong() => SetStatusText("顺序不对，再试试！");
        public void OnSequenceComplete() => SetStatusText("恭喜！梦境即将结束...");

        public void OnLevelComplete()
        {
            if (panelRoot != null)
                panelRoot.SetActive(false);
        }

        // --- 内部 ---

        private IEnumerator ScrollCountCoroutine(int target, float duration)
        {
            var start = displayedCount;
            var elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                var t = 1f - Mathf.Pow(1f - Mathf.Clamp01(elapsed / duration), 3f);
                displayedCount = Mathf.RoundToInt(Mathf.Lerp(start, target, t));
                followerCountText.text = FormatCount(displayedCount);
                yield return null;
            }
            displayedCount = target;
            followerCountText.text = FormatCount(target);
            scrollRoutine = null;
        }

        private void SetStatusText(string text)
        {
            if (statusText != null)
                statusText.text = text;
        }

        private static string FormatCount(int count)
        {
            if (count >= 10000)
                return $"{count / 10000f:F1}万";
            return count.ToString("N0");
        }
    }
}
