using GameCreate3.Core;
using GameCreate3.Core.SceneRouting;
using UnityEngine;
using UnityEngine.UI;

namespace GameCreate3.UI
{
    public sealed class UIPausePageController : UIPageController
    {
        [SerializeField] private Button resumeButton;
        [SerializeField] private Button restartButton;
        [SerializeField] private Button settingsButton;
        [SerializeField] private Button mainMenuButton;

        private void OnEnable()
        {
            Add(resumeButton, HandleResume);
            Add(restartButton, HandleRestart);
            Add(settingsButton, HandleSettings);
            Add(mainMenuButton, HandleMainMenu);
        }

        private void OnDisable()
        {
            Remove(resumeButton, HandleResume);
            Remove(restartButton, HandleRestart);
            Remove(settingsButton, HandleSettings);
            Remove(mainMenuButton, HandleMainMenu);
        }

        private void HandleResume()
        {
            UIControlSystem.Instance?.ClosePage(PageId);
        }

        private static void HandleRestart()
        {
            SceneRouter.Reload();
        }

        private static void HandleSettings()
        {
            UIControlSystem.Instance?.OpenPage(UIPageIds.Settings);
        }

        private static void HandleMainMenu()
        {
            var data = new UIPromptPopupData
            {
                title = "Back to menu",
                message = "Return to the main menu?",
                confirmText = "OK",
                cancelText = "Cancel",
                showCancel = true,
                onConfirm = () => SceneRouter.Go("main_menu")
            };

            UIControlSystem.Instance?.PushPopup(UIPageIds.ConfirmPopup, data);
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
