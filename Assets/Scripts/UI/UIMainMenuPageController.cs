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
            if (continueButton != null)
            {
                continueButton.interactable = HasProgress();
            }
        }

        private static bool HasProgress()
        {
            var save = GameSaveProgressService.Instance;
            return save != null && save.TryGetProgressBool("has_progress", false);
        }

        private static void HandleStart()
        {
            SceneRouter.Go("start_new_game");
        }

        private static void HandleContinue()
        {
            SceneRouter.Go("continue_game");
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
