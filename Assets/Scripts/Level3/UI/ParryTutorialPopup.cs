using UnityEngine;
using UnityEngine.UI;

namespace GameCreate3.Level3
{
    public sealed class ParryTutorialPopup : MonoBehaviour
    {
        [SerializeField] private GameObject redDot;
        [SerializeField] private GameObject popupPanel;
        [SerializeField] private AudioClip vibrationClip;

        private bool hasOpened;
        private GameObject dimmer;
        private Canvas panelOverrideCanvas;

        private bool attentionActive;
        private Vector3 originalScale;
        private Vector2 originalAnchoredPos;
        private float attentionTimer;
        private AudioSource vibrationSource;
        private RectTransform rt;

        private void Awake()
        {
            if (popupPanel != null) popupPanel.SetActive(false);
            rt = GetComponent<RectTransform>();
            originalScale = transform.localScale;
            originalAnchoredPos = rt != null ? rt.anchoredPosition : (Vector2)transform.localPosition;
        }

        public void Show()
        {
            Debug.Log($"[ParryTutorialPopup] Show() called, hasOpened={hasOpened}, clip={vibrationClip != null}, active={gameObject.activeInHierarchy}");
            if (hasOpened) return;
            if (redDot != null) redDot.SetActive(true);

            attentionActive = true;
            attentionTimer = 0f;

            if (vibrationClip != null && vibrationSource == null)
            {
                vibrationSource = gameObject.AddComponent<AudioSource>();
                vibrationSource.clip = vibrationClip;
                vibrationSource.loop = true;
                vibrationSource.playOnAwake = false;
                vibrationSource.volume = 1f;
                vibrationSource.spatialBlend = 0f;
                vibrationSource.Play();
                Debug.Log($"[ParryTutorialPopup] Audio started, isPlaying={vibrationSource.isPlaying}");
            }
        }

        private void Update()
        {
            if (!attentionActive) return;

            attentionTimer += Time.unscaledDeltaTime;

            float pulse = 1f + 0.08f * Mathf.Sin(attentionTimer * 5f);
            transform.localScale = originalScale * pulse;

            float shakeX = Mathf.Sin(attentionTimer * 23f) * 2f;
            float shakeY = Mathf.Cos(attentionTimer * 19f) * 1.5f;
            if (rt != null)
                rt.anchoredPosition = originalAnchoredPos + new Vector2(shakeX, shakeY);
            else
                transform.localPosition = (Vector3)originalAnchoredPos + new Vector3(shakeX, shakeY, 0f);
        }

        private void StopAttention()
        {
            attentionActive = false;
            transform.localScale = originalScale;
            if (rt != null)
                rt.anchoredPosition = originalAnchoredPos;
            else
                transform.localPosition = (Vector3)originalAnchoredPos;

            if (vibrationSource != null)
            {
                vibrationSource.Stop();
                Destroy(vibrationSource);
                vibrationSource = null;
            }
        }

        public void Open()
        {
            hasOpened = true;
            StopAttention();
            if (redDot != null) redDot.SetActive(false);

            var canvas = GetComponentInParent<Canvas>();
            if (canvas == null) return;

            dimmer = new GameObject("TutorialDimmer");
            dimmer.transform.SetParent(canvas.transform, false);
            var rt = dimmer.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            var img = dimmer.AddComponent<Image>();
            img.color = new Color(0f, 0f, 0f, 0.7f);
            var btn = dimmer.AddComponent<Button>();
            btn.transition = Selectable.Transition.None;
            btn.onClick.AddListener(Close);

            if (popupPanel != null)
            {
                popupPanel.SetActive(true);
                panelOverrideCanvas = popupPanel.GetComponent<Canvas>();
                if (panelOverrideCanvas == null)
                    panelOverrideCanvas = popupPanel.AddComponent<Canvas>();
                panelOverrideCanvas.overrideSorting = true;
                panelOverrideCanvas.sortingOrder = canvas.sortingOrder + 2;
                if (popupPanel.GetComponent<GraphicRaycaster>() == null)
                    popupPanel.AddComponent<GraphicRaycaster>();
                var cg = popupPanel.GetComponent<CanvasGroup>();
                if (cg == null) cg = popupPanel.AddComponent<CanvasGroup>();
                cg.blocksRaycasts = false;
            }

            Time.timeScale = 0f;
        }

        private void Close()
        {
            if (dimmer != null) Destroy(dimmer);
            if (popupPanel != null)
            {
                popupPanel.SetActive(false);
                if (panelOverrideCanvas != null)
                    panelOverrideCanvas.overrideSorting = false;
            }
            Time.timeScale = 1f;
        }
    }
}
