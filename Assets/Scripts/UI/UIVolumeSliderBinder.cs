using UnityEngine;
using UnityEngine.UI;
using GameCreate3.Core;

namespace GameCreate3.UI
{
    public sealed class UIVolumeSliderBinder : MonoBehaviour
    {
        [SerializeField] private UIVolumeChannel channel;
        [SerializeField] private Slider slider;
        [SerializeField] private UISettingsService settingsService;

        private void Awake()
        {
            if (slider == null)
            {
                slider = GetComponent<Slider>();
            }
        }

        private void OnEnable()
        {
            if (settingsService == null)
            {
                settingsService = UISettingsService.Instance;
            }

            if (settingsService != null && slider != null)
            {
                slider.SetValueWithoutNotify(settingsService.GetVolume(channel));
                slider.onValueChanged.AddListener(HandleValueChanged);
                settingsService.OnVolumeSettingsChanged += HandleSettingsChanged;
            }
        }

        private void OnDisable()
        {
            if (slider != null)
            {
                slider.onValueChanged.RemoveListener(HandleValueChanged);
            }

            if (settingsService != null)
            {
                settingsService.OnVolumeSettingsChanged -= HandleSettingsChanged;
            }
        }

        private void HandleValueChanged(float value)
        {
            settingsService?.SetVolume(channel, value);
            GameAudioService.Instance?.SetVolume(UISettingsService.ToGameAudioChannel(channel), value);
        }

        private void HandleSettingsChanged(UIVolumeSettings settings)
        {
            if (slider != null && settingsService != null)
            {
                slider.SetValueWithoutNotify(settingsService.GetVolume(channel));
            }
        }
    }
}
