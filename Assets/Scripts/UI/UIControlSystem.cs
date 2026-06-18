using System;
using System.Collections.Generic;
using GameCreate3.Core.SceneRouting;
using UnityEngine;
using UnityEngine.EventSystems;

namespace GameCreate3.UI
{
    public sealed class UIControlSystem : MonoBehaviour
    {
        [Serializable]
        private sealed class PagePrefabEntry
        {
            public string pageId;
            public UIPageController prefab;
            public UIPageLayer layer = UIPageLayer.Menu;
            public bool closePeersOnOpen = true;
        }

        [Serializable]
        private sealed class PopupPrefabEntry
        {
            public string popupId;
            public UIPageController prefab;
        }

        [Header("Singleton")]
        [SerializeField] private bool dontDestroyOnLoad = true;

        [Header("Roots")]
        [SerializeField] private Transform mainRoot;
        [SerializeField] private Transform hudRoot;
        [SerializeField] private Transform menuRoot;
        [SerializeField] private Transform popupRoot;
        [SerializeField] private Transform overlayRoot;

        [Header("Pages")]
        [SerializeField] private List<UIPageController> scenePages = new List<UIPageController>();
        [SerializeField] private List<PagePrefabEntry> pagePrefabs = new List<PagePrefabEntry>();
        [SerializeField] private List<PopupPrefabEntry> popupPrefabs = new List<PopupPrefabEntry>();

        [Header("HUD")]
        [SerializeField] private CanvasGroup hudGroup;

        [Header("Input")]
        [SerializeField] private bool ensureEventSystem = true;

        [Header("Startup")]
        [Tooltip("进入 Play 后自动打开的页面，例如主菜单场景填 main_menu。")]
        [SerializeField] private string startupPageId;

        private readonly Dictionary<string, UIPageController> pages = new Dictionary<string, UIPageController>();
        private readonly Dictionary<string, PagePrefabEntry> pagePrefabMap = new Dictionary<string, PagePrefabEntry>();
        private readonly Dictionary<string, PopupPrefabEntry> popupPrefabMap = new Dictionary<string, PopupPrefabEntry>();
        private readonly List<UIPageController> popupStack = new List<UIPageController>();

        private UIInputFocusTarget currentInputFocus = UIInputFocusTarget.Gameplay;

        public static UIControlSystem Instance { get; private set; }
        public UIInputFocusTarget CurrentInputFocus => currentInputFocus;

        public event Action<string, UIPageController> OnPageOpened;
        public event Action<string, UIPageController> OnPageClosed;
        public event Action<UIInputFocusTarget> OnInputFocusChanged;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            if (dontDestroyOnLoad)
            {
                DontDestroyOnLoad(gameObject);
            }

            if (ensureEventSystem)
            {
                EnsureEventSystem();
            }

            RegisterPrefabMaps();
            RegisterScenePages();
            SceneRouter.OnAfterChange += HandleSceneChanged;
        }

        private void Start()
        {
            TryOpenStartupPage();
        }

        private void OnDestroy()
        {
            SceneRouter.OnAfterChange -= HandleSceneChanged;
            if (Instance == this)
            {
                Instance = null;
            }
        }

        private void HandleSceneChanged(SceneRouteContext ctx)
        {
            // 切回主菜单等场景时，重新发现场景里新创建的 UIPageController
            RegisterScenePages();
            TryOpenStartupPage();
        }

        public UIPageController OpenPage(string pageId)
        {
            return OpenPage(pageId, null);
        }

        public void TryOpenStartupPage()
        {
            var pageId = ResolveStartupPageId();
            if (string.IsNullOrWhiteSpace(pageId))
            {
                return;
            }

            var page = OpenPage(pageId);
            if (page == null)
            {
                Debug.LogError(
                    $"[UIControlSystem] Startup page '{pageId}' failed to open. " +
                    "Put MainMenuPage under MenuRoot and add it to Scene Pages, or run GameCreate3/Setup MainMenu Scene.");
            }
        }

        public UIPageController OpenPage(string pageId, object data)
        {
            if (ensureEventSystem)
            {
                EnsureEventSystem();
            }

            if (!TryGetOrCreatePage(pageId, out var page))
            {
                Debug.LogWarning($"[UIControlSystem] Page not found: {pageId}");
                return null;
            }

            if (page.ClosePeersOnOpen)
            {
                CloseLayerPeers(page);
            }

            page.Open(data);
            SetInputFocus(page.Layer == UIPageLayer.HUD ? UIInputFocusTarget.Gameplay : UIInputFocusTarget.UI);
            OnPageOpened?.Invoke(pageId, page);
            return page;
        }

        public void ClosePage(string pageId)
        {
            if (string.IsNullOrEmpty(pageId))
            {
                return;
            }

            if (pages.TryGetValue(pageId, out var page) && page != null)
            {
                page.Close();
                OnPageClosed?.Invoke(pageId, page);
            }

            CloseMatchingPopups(pageId);
            RestoreInputFocusAfterClose();
        }

        public UIPageController PushPopup(string popupId, object data)
        {
            if (string.IsNullOrEmpty(popupId))
            {
                Debug.LogWarning("[UIControlSystem] Popup id is empty.");
                return null;
            }

            var popup = CreatePopup(popupId);
            if (popup == null)
            {
                Debug.LogWarning($"[UIControlSystem] Popup not found: {popupId}");
                return null;
            }

            popupStack.Add(popup);
            popup.Open(data);
            SetInputFocus(UIInputFocusTarget.Popup);
            OnPageOpened?.Invoke(popupId, popup);
            return popup;
        }

        public void PopPopup()
        {
            if (popupStack.Count == 0)
            {
                RestoreInputFocusAfterClose();
                return;
            }

            var popup = popupStack[popupStack.Count - 1];
            popupStack.RemoveAt(popupStack.Count - 1);
            ClosePopupInstance(popup);
            RestoreInputFocusAfterClose();
        }

        public void SetHUDVisible(bool visible)
        {
            if (hudGroup != null)
            {
                hudGroup.alpha = visible ? 1f : 0f;
                hudGroup.interactable = visible;
                hudGroup.blocksRaycasts = visible;
            }

            foreach (var pair in pages)
            {
                var page = pair.Value;
                if (page != null && page.Layer == UIPageLayer.HUD)
                {
                    if (visible)
                    {
                        page.Open();
                    }
                    else
                    {
                        page.Close();
                    }
                }
            }
        }

        public void SetInputFocus(UIInputFocusTarget target)
        {
            currentInputFocus = target;
            if (target == UIInputFocusTarget.Gameplay && EventSystem.current != null)
            {
                EventSystem.current.SetSelectedGameObject(null);
            }

            OnInputFocusChanged?.Invoke(target);
        }

        public void SetInputFocus(GameObject target)
        {
            currentInputFocus = target == null ? UIInputFocusTarget.Gameplay : UIInputFocusTarget.UI;
            if (EventSystem.current != null)
            {
                EventSystem.current.SetSelectedGameObject(target);
            }

            OnInputFocusChanged?.Invoke(currentInputFocus);
        }

        public void RefreshPage(string pageId, object data)
        {
            if (pages.TryGetValue(pageId, out var page) && page != null)
            {
                page.Refresh(data);
            }
        }

        private void RegisterPrefabMaps()
        {
            pagePrefabMap.Clear();
            for (var i = 0; i < pagePrefabs.Count; i++)
            {
                var entry = pagePrefabs[i];
                if (entry == null || string.IsNullOrEmpty(entry.pageId) || entry.prefab == null)
                {
                    continue;
                }

                pagePrefabMap[entry.pageId] = entry;
            }

            popupPrefabMap.Clear();
            for (var i = 0; i < popupPrefabs.Count; i++)
            {
                var entry = popupPrefabs[i];
                if (entry == null || string.IsNullOrEmpty(entry.popupId) || entry.prefab == null)
                {
                    continue;
                }

                popupPrefabMap[entry.popupId] = entry;
            }
        }

        private void RegisterScenePages()
        {
            for (var i = 0; i < scenePages.Count; i++)
            {
                RegisterPage(scenePages[i]);
            }

            var discoveredPages = FindObjectsOfType<UIPageController>(true);
            for (var i = 0; i < discoveredPages.Length; i++)
            {
                RegisterPage(discoveredPages[i]);
            }
        }

        private void RegisterPage(UIPageController page)
        {
            if (page == null || string.IsNullOrEmpty(page.PageId))
            {
                return;
            }

            pages[page.PageId] = page;
        }

        private string ResolveStartupPageId()
        {
            if (!string.IsNullOrWhiteSpace(startupPageId))
            {
                return startupPageId;
            }

            // Main menu scenes should still show even if the instance forgot to
            // serialize Startup Page Id. This keeps Play mode from showing an
            // invisible UIControlSystem with all pages hidden by Hide On Awake.
            return pages.ContainsKey(UIPageIds.MainMenu) ? UIPageIds.MainMenu : string.Empty;
        }

        private bool TryGetOrCreatePage(string pageId, out UIPageController page)
        {
            page = null;
            if (string.IsNullOrEmpty(pageId))
            {
                return false;
            }

            if (pages.TryGetValue(pageId, out page) && page != null)
            {
                return true;
            }

            if (!pagePrefabMap.TryGetValue(pageId, out var entry) || entry.prefab == null)
            {
                return false;
            }

            page = Instantiate(entry.prefab, ResolveLayerRoot(entry.layer));
            if (string.IsNullOrEmpty(page.PageId))
            {
                page.Configure(pageId, entry.layer, entry.closePeersOnOpen);
            }

            RegisterPage(page);
            return true;
        }

        private UIPageController CreatePopup(string popupId)
        {
            if (!popupPrefabMap.TryGetValue(popupId, out var entry) || entry.prefab == null)
            {
                return null;
            }

            var popup = Instantiate(entry.prefab, ResolveLayerRoot(UIPageLayer.Popup));
            if (string.IsNullOrEmpty(popup.PageId))
            {
                popup.Configure(popupId, UIPageLayer.Popup, false);
            }

            return popup;
        }

        private void CloseLayerPeers(UIPageController openedPage)
        {
            foreach (var pair in pages)
            {
                var page = pair.Value;
                if (page == null || page == openedPage || page.Layer != openedPage.Layer || !page.IsOpen)
                {
                    continue;
                }

                page.Close();
                OnPageClosed?.Invoke(pair.Key, page);
            }
        }

        private void CloseMatchingPopups(string popupId)
        {
            for (var i = popupStack.Count - 1; i >= 0; i--)
            {
                var popup = popupStack[i];
                if (popup == null || popup.PageId != popupId)
                {
                    continue;
                }

                popupStack.RemoveAt(i);
                ClosePopupInstance(popup);
            }
        }

        private void ClosePopupInstance(UIPageController popup)
        {
            if (popup == null)
            {
                return;
            }

            var popupId = popup.PageId;
            popup.Close();
            OnPageClosed?.Invoke(popupId, popup);
            Destroy(popup.gameObject);
        }

        private void RestoreInputFocusAfterClose()
        {
            if (popupStack.Count > 0)
            {
                SetInputFocus(UIInputFocusTarget.Popup);
                return;
            }

            foreach (var pair in pages)
            {
                var page = pair.Value;
                if (page != null && page.IsOpen && page.Layer != UIPageLayer.HUD)
                {
                    SetInputFocus(UIInputFocusTarget.UI);
                    return;
                }
            }

            SetInputFocus(UIInputFocusTarget.Gameplay);
        }

        private Transform ResolveLayerRoot(UIPageLayer layer)
        {
            switch (layer)
            {
                case UIPageLayer.Main:
                    return mainRoot != null ? mainRoot : transform;
                case UIPageLayer.HUD:
                    return hudRoot != null ? hudRoot : transform;
                case UIPageLayer.Popup:
                    return popupRoot != null ? popupRoot : transform;
                case UIPageLayer.Overlay:
                    return overlayRoot != null ? overlayRoot : transform;
                default:
                    return menuRoot != null ? menuRoot : transform;
            }
        }

        private static void EnsureEventSystem()
        {
            if (EventSystem.current != null)
            {
                return;
            }

            var eventSystemObject = new GameObject("EventSystem");
            eventSystemObject.AddComponent<EventSystem>();
            eventSystemObject.AddComponent<StandaloneInputModule>();
        }
    }
}
