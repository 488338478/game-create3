using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace GameCreate3.UI
{
    public enum UIPageLayer
    {
        Main,
        HUD,
        Menu,
        Popup,
        Overlay
    }

    public enum UIInputFocusTarget
    {
        None,
        Gameplay,
        UI,
        Popup
    }

    public class UIPageController : MonoBehaviour
    {
        [Header("Page")]
        [SerializeField] private string pageId;
        [SerializeField] private UIPageLayer layer = UIPageLayer.Menu;
        [SerializeField] private bool closePeersOnOpen = true;
        [SerializeField] private bool hideOnAwake = true;

        [Header("View")]
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private GameObject firstSelected;

        private object currentData;

        public string PageId => pageId;
        public UIPageLayer Layer => layer;
        public bool ClosePeersOnOpen => closePeersOnOpen;
        public bool IsOpen { get; private set; }
        public object CurrentData => currentData;

        public event Action<UIPageController, object> Opened;
        public event Action<UIPageController> Closed;
        public event Action<UIPageController, object> Refreshed;

        protected virtual void Awake()
        {
            if (canvasGroup == null)
            {
                canvasGroup = GetComponent<CanvasGroup>();
            }

            if (hideOnAwake)
            {
                SetVisible(false);
            }
        }

        public void Configure(string id, UIPageLayer targetLayer, bool closeLayerPeers)
        {
            pageId = id;
            layer = targetLayer;
            closePeersOnOpen = closeLayerPeers;
        }

        public void Open(object data = null)
        {
            currentData = data;
            gameObject.SetActive(true);
            SetVisible(true);
            IsOpen = true;

            OnOpened(data);
            Opened?.Invoke(this, data);
            FocusFirstSelectable();
        }

        public void Close()
        {
            if (!IsOpen && !gameObject.activeSelf)
            {
                return;
            }

            IsOpen = false;
            SetVisible(false);
            OnClosed();
            Closed?.Invoke(this);
        }

        public void Refresh(object data = null)
        {
            currentData = data;
            OnRefreshed(data);
            Refreshed?.Invoke(this, data);
        }

        public void SetInteractable(bool interactable)
        {
            if (canvasGroup != null)
            {
                canvasGroup.interactable = interactable;
                canvasGroup.blocksRaycasts = interactable;
            }
        }

        protected virtual void OnOpened(object data)
        {
        }

        protected virtual void OnClosed()
        {
        }

        protected virtual void OnRefreshed(object data)
        {
        }

        private void SetVisible(bool visible)
        {
            if (canvasGroup == null)
            {
                gameObject.SetActive(visible);
                return;
            }

            canvasGroup.alpha = visible ? 1f : 0f;
            canvasGroup.interactable = visible;
            canvasGroup.blocksRaycasts = visible;
        }

        private void FocusFirstSelectable()
        {
            if (firstSelected == null || EventSystem.current == null)
            {
                return;
            }

            EventSystem.current.SetSelectedGameObject(firstSelected);
        }
    }
}
