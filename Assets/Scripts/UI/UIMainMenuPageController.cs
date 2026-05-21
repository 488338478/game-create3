using GameCreate3.Core;
using GameCreate3.Core.SceneRouting;
using UnityEngine;
using UnityEngine.UI;

namespace GameCreate3.UI
{
    public sealed class UIMainMenuPageController : UIPageController
    {
        [SerializeField] private Button startButton;
        [SerializeField] private Button continueButton;
        [SerializeField] private Button settingsButton;
        [SerializeField] private Button galleryButton;
        [SerializeField] private Button exitButton;

        private void OnEnable()
        {
            Add(startButton, HandleStart);
            Add(continueButton, HandleContinue);
            Add(settingsButton, HandleSettings);
            Add(galleryButton, HandleGallery);
            Add(exitButton, HandleExit);
            UpdateContinueButtonState();
        }

        private void OnDisable()
        {
            Remove(startButton, HandleStart);
            Remove(continueButton, HandleContinue);
            Remove(settingsButton, HandleSettings);
            Remove(galleryButton, HandleGallery);
            Remove(exitButton, HandleExit);
        }

        protected override void OnOpened(object data)
        {
            UpdateContinueButtonState();
        }

        private void UpdateContinueButtonState()
        {
            if (continueButton != null)
            {
                continueButton.interactable = HasProgress();
            }
        }

        private static bool HasProgress()
        {
            var save = GameSaveProgressService.Instance;
            return save != null && save.TryGetProgressBool(UIProgressKeys.HasProgress, false);
        }

        private static void HandleStart()
        {
            UIControlSystem.Instance?.OpenPage(UIPageIds.LevelSelect);
        }

        private static void HandleContinue()
        {
            var save = GameSaveProgressService.Instance;
            var routeId = save != null ? save.GetProgress(UIProgressKeys.LastRouteId, string.Empty) : string.Empty;
            if (!string.IsNullOrWhiteSpace(routeId))
            {
                SceneRouter.Go(routeId);
                return;
            }

            UIControlSystem.Instance?.OpenPage(UIPageIds.LevelSelect);
        }

        private static void HandleSettings()
        {
            UIControlSystem.Instance?.OpenPage(UIPageIds.Settings);
        }

        private static void HandleGallery()
        {
            UIControlSystem.Instance?.OpenPage(UIPageIds.CGGallery);
        }

        private static void HandleExit()
        {
            Application.Quit();
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
