using UnityEngine;
using UnityEngine.UI;

namespace GameCreate3.Level3
{
    public sealed class ParryTutorialPopup : MonoBehaviour
    {
        [SerializeField] private GameObject redDot;
        [SerializeField] private GameObject popupPanel;

        private bool hasOpened;
        private GameObject dimmer;
        private Canvas panelOverrideCanvas;

        private void Awake()
        {
            if (popupPanel != null) popupPanel.SetActive(false);
        }

        public void Show()
        {
            if (hasOpened) return;
            if (redDot != null) redDot.SetActive(true);
        }

        public void Open()
        {
            hasOpened = true;
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
