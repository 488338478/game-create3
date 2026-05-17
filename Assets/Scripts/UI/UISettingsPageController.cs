using GameCreate3.Core;
using UnityEngine;
using UnityEngine.UI;

namespace GameCreate3.UI
{
    public sealed class UISettingsPageController : UIPageController
    {
        [SerializeField] private Button resetButton;
        [SerializeField] private Button backButton;

        private void OnEnable()
        {
            if (resetButton != null)
            {
                resetButton.onClick.AddListener(HandleReset);
            }

            if (backButton != null)
            {
                backButton.onClick.AddListener(HandleBack);
            }
        }

        private void OnDisable()
        {
            if (resetButton != null)
            {
                resetButton.onClick.RemoveListener(HandleReset);
            }

            if (backButton != null)
            {
                backButton.onClick.RemoveListener(HandleBack);
            }
        }

        private static void HandleReset()
        {
            UISettingsService.Instance?.ResetVolumeDefaults();
            GameAudioService.Instance?.SetVolume(GameAudioChannel.Master, 1f);
            GameAudioService.Instance?.SetVolume(GameAudioChannel.Bgm, 1f);
            GameAudioService.Instance?.SetVolume(GameAudioChannel.Sfx, 1f);
            GameAudioService.Instance?.SetVolume(GameAudioChannel.Ui, 1f);
        }

        private void HandleBack()
        {
            UISettingsService.Instance?.Save();
            Close();
        }
    }
}
