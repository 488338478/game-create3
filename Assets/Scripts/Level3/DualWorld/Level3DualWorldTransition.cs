using System.Collections;
using UnityEngine;

namespace GameCreate3.Level3
{
    public sealed class Level3DualWorldTransition : MonoBehaviour
    {
        [Header("Xiaohongshu Panel (slides in from right)")]
        [SerializeField] private RectTransform xiaohongshuPanel;

        [Header("Transition")]
        [SerializeField] private float transitionDuration = 1.2f;
        [SerializeField] private AnimationCurve easeCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

        [Header("Offsets")]
        [SerializeField] private float panelOffscreenX = 960f;

        private bool transitioned;
        private Vector2 panelOriginalPos;

        private void Awake()
        {
            if (xiaohongshuPanel != null)
            {
                panelOriginalPos = xiaohongshuPanel.anchoredPosition;
                xiaohongshuPanel.anchoredPosition = new Vector2(panelOffscreenX, panelOriginalPos.y);
                xiaohongshuPanel.gameObject.SetActive(false);
            }
        }

        public void OnPhase2()
        {
            if (transitioned) return;
            transitioned = true;
            StartCoroutine(PlayTransition());
        }

        private IEnumerator PlayTransition()
        {
            if (xiaohongshuPanel == null) yield break;

            xiaohongshuPanel.gameObject.SetActive(true);

            var elapsed = 0f;
            var panelStart = xiaohongshuPanel.anchoredPosition;
            var panelTarget = panelOriginalPos;

            while (elapsed < transitionDuration)
            {
                elapsed += Time.deltaTime;
                var t = easeCurve.Evaluate(Mathf.Clamp01(elapsed / transitionDuration));
                xiaohongshuPanel.anchoredPosition = Vector2.Lerp(panelStart, panelTarget, t);
                yield return null;
            }

            xiaohongshuPanel.anchoredPosition = panelTarget;
        }
    }
}
