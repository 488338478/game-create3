using GameCreate3.Core.SceneRouting;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace GameCreate3.UI
{
    public sealed class UIMainMenuPageController : UIPageController, IPointerClickHandler
    {
        [Header("Landing")]
        [SerializeField] private Image screenClickTarget;
        [SerializeField] private Image backgroundImage;
        [SerializeField] private GameObject[] mainMenuObjects;
        [SerializeField] private Color normalBackgroundColor = Color.white;
        [SerializeField] private Color menuOpenBackgroundColor = new Color(0.35f, 0.35f, 0.35f, 1f);
        [SerializeField] private Button enterButton;
        [SerializeField] private Button menuButton;

        [Header("Menu Overlay")]
        [SerializeField] private GameObject menuRoot;
        [SerializeField] private GameObject[] menuObjects;
        [SerializeField] private RectTransform selectionMarker;
        [SerializeField] private Vector2 selectionMarkerOffset = new Vector2(0f, 38f);
        [SerializeField] private Button startButton;
        [SerializeField] private Button continueButton;
        [SerializeField] private Button exitButton;
        [SerializeField] private bool closeMenuAfterStart;

        private bool isMenuOpen;
        private bool isMainMenuVisible = true;

        private void OnEnable()
        {
            Add(enterButton, HandleStart);
            Add(menuButton, HandleMenu);
            Add(startButton, HandleStart);
            Add(continueButton, HandleContinue);
            Add(exitButton, HandleExit);
            AddSelectionEvents(startButton);
            AddSelectionEvents(continueButton);
            AddSelectionEvents(exitButton);
            SceneRouter.OnAfterChange += HandleSceneChanged;
            UpdateContinueButtonState();
            SetMainMenuVisible(true);
        }

        private void OnDisable()
        {
            Remove(enterButton, HandleStart);
            Remove(menuButton, HandleMenu);
            Remove(startButton, HandleStart);
            Remove(continueButton, HandleContinue);
            Remove(exitButton, HandleExit);
            RemoveSelectionEvents(startButton);
            RemoveSelectionEvents(continueButton);
            RemoveSelectionEvents(exitButton);
            SceneRouter.OnAfterChange -= HandleSceneChanged;
        }

        protected override void OnOpened(object data)
        {
            UpdateContinueButtonState();
            SetMainMenuVisible(true);
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (isMainMenuVisible && !isMenuOpen)
            {
                HandleStart();
            }
        }

        private void UpdateContinueButtonState()
        {
            if (continueButton != null)
            {
                continueButton.interactable = true;
            }
        }

        private void HandleMenu()
        {
            SetMenuOpen(!isMenuOpen);
        }

        private void HandleStart()
        {
            SetMainMenuVisible(false);
            if (closeMenuAfterStart || isMenuOpen)
            {
                SetMenuOpen(false);
            }

            transform.SetAsLastSibling();
            UIControlSystem.Instance?.OpenPage(UIPageIds.LevelSelect);
            transform.SetAsLastSibling();
        }

        private void HandleContinue()
        {
            SetMenuOpen(false);
        }

        private void SetMenuOpen(bool open)
        {
            isMenuOpen = open;

            if (menuRoot != null)
            {
                menuRoot.SetActive(open);
            }

            if (menuObjects != null)
            {
                for (var i = 0; i < menuObjects.Length; i++)
                {
                    if (menuObjects[i] != null)
                    {
                        menuObjects[i].SetActive(open);
                    }
                }
            }

            if (backgroundImage != null)
            {
                backgroundImage.color = open && !isMainMenuVisible ? menuOpenBackgroundColor : normalBackgroundColor;
            }

            if (selectionMarker != null)
            {
                selectionMarker.gameObject.SetActive(open);
            }

            if (open)
            {
                MoveSelectionMarker(startButton);
            }
        }

        private void HandleExit()
        {
            SetMainMenuVisible(true);
            UIControlSystem.Instance?.ClosePage(UIPageIds.LevelSelect);
            SceneRouter.Go("main_menu");
        }

        private void HandleSceneChanged(SceneRouteContext context)
        {
            if (context.ToRouteId == "main_menu")
            {
                UIControlSystem.Instance?.OpenPage(UIPageIds.MainMenu);
                UIControlSystem.Instance?.SetInputFocus(gameObject);
                SetMainMenuVisible(true);
            }
        }

        private void SetMainMenuVisible(bool visible)
        {
            isMainMenuVisible = visible;
            SetMenuOpen(false);

            if (mainMenuObjects != null)
            {
                for (var i = 0; i < mainMenuObjects.Length; i++)
                {
                    if (mainMenuObjects[i] != null)
                    {
                        mainMenuObjects[i].SetActive(visible);
                    }
                }
            }

            if (backgroundImage != null)
            {
                backgroundImage.gameObject.SetActive(visible);
                backgroundImage.color = normalBackgroundColor;
            }

            if (enterButton != null)
            {
                enterButton.gameObject.SetActive(false);
            }

            if (menuButton != null)
            {
                menuButton.gameObject.SetActive(!visible);
            }

            var clickTarget = screenClickTarget != null ? screenClickTarget : GetComponent<Image>();
            if (clickTarget != null)
            {
                clickTarget.raycastTarget = visible;
            }
        }

        private void AddSelectionEvents(Button button)
        {
            if (button == null)
            {
                return;
            }

            var trigger = button.GetComponent<EventTrigger>() ?? button.gameObject.AddComponent<EventTrigger>();
            AddTrigger(trigger, EventTriggerType.PointerEnter, () => MoveSelectionMarker(button));
            AddTrigger(trigger, EventTriggerType.Select, () => MoveSelectionMarker(button));
        }

        private static void RemoveSelectionEvents(Button button)
        {
            var trigger = button != null ? button.GetComponent<EventTrigger>() : null;
            if (trigger != null)
            {
                trigger.triggers.Clear();
            }
        }

        private static void AddTrigger(EventTrigger trigger, EventTriggerType type, UnityEngine.Events.UnityAction action)
        {
            var entry = new EventTrigger.Entry { eventID = type };
            entry.callback.AddListener(_ => action());
            trigger.triggers.Add(entry);
        }

        private void MoveSelectionMarker(Button target)
        {
            if (selectionMarker == null || target == null)
            {
                return;
            }

            var targetTransform = target.transform as RectTransform;
            var markerParent = selectionMarker.parent as RectTransform;
            if (targetTransform == null || markerParent == null)
            {
                return;
            }

            selectionMarker.gameObject.SetActive(true);
            selectionMarker.SetParent(markerParent, false);
            selectionMarker.anchorMin = targetTransform.anchorMin;
            selectionMarker.anchorMax = targetTransform.anchorMax;
            selectionMarker.pivot = targetTransform.pivot;
            selectionMarker.anchoredPosition = targetTransform.anchoredPosition + selectionMarkerOffset;
        }

        private static void Add(Button button, UnityEngine.Events.UnityAction action)
        {
            if (button != null)
            {
                button.onClick.AddListener(action);
            }
        }

        private static void Remove(Button button, UnityEngine.Events.UnityAction action)
        {
            if (button != null)
            {
                button.onClick.RemoveListener(action);
            }
        }
    }
}
