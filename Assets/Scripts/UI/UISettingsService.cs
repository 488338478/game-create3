using System;
using GameCreate3.Core;
using GameCreate3.StoryPlayer;
using UnityEngine;

namespace GameCreate3.UI
{
    public enum UIVolumeChannel
    {
        Master,
        Bgm,
        Sfx,
        Ui,
        Voice
    }

    [Serializable]
    public sealed class UIVolumeSettings
    {
        public float master = 1f;
        public float bgm = 1f;
        public float sfx = 1f;
        public float ui = 1f;
        public float voice = 1f;
    }

    public sealed class UISettingsService : MonoBehaviour
    {
        private const string MasterVolumeKey = "Master_Volume";
        private const string BgmVolumeKey = "BGM_Volume";
        private const string SfxVolumeKey = "SFX_Volume";
        private const string UiSfxVolumeKey = "UI_SFX_Volume";
        private const string VoiceVolumeKey = "Voice_Volume";

        [SerializeField] private UIVolumeSettings volumeSettings = new UIVolumeSettings();
        [SerializeField] private bool loadOnAwake = true;

        public static UISettingsService Instance { get; private set; }
        public UIVolumeSettings VolumeSettings => volumeSettings;

        public event Action<UIVolumeSettings> OnVolumeSettingsChanged;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            if (loadOnAwake)
            {
                Load();
            }
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        public void Load()
        {
            volumeSettings.master = PlayerPrefs.GetFloat(MasterVolumeKey, 1f);
            volumeSettings.bgm = PlayerPrefs.GetFloat(BgmVolumeKey, 1f);
            volumeSettings.sfx = PlayerPrefs.GetFloat(SfxVolumeKey, 1f);
            volumeSettings.ui = PlayerPrefs.GetFloat(UiSfxVolumeKey, PlayerPrefs.GetFloat(SfxVolumeKey, 1f));
            volumeSettings.voice = PlayerPrefs.GetFloat(VoiceVolumeKey, 1f);
            ApplyVolumeSettings(false);
        }

        public void Save()
        {
            PlayerPrefs.SetFloat(MasterVolumeKey, volumeSettings.master);
            PlayerPrefs.SetFloat(BgmVolumeKey, volumeSettings.bgm);
            PlayerPrefs.SetFloat(SfxVolumeKey, volumeSettings.sfx);
            PlayerPrefs.SetFloat(UiSfxVolumeKey, volumeSettings.ui);
            PlayerPrefs.SetFloat(VoiceVolumeKey, volumeSettings.voice);
            PlayerPrefs.Save();
        }

        public void ResetVolumeDefaults()
        {
            volumeSettings.master = 1f;
            volumeSettings.bgm = 1f;
            volumeSettings.sfx = 1f;
            volumeSettings.ui = 1f;
            volumeSettings.voice = 1f;
            ApplyVolumeSettings(true);
        }

        public void SetMasterVolume(float value)
        {
            SetVolume(UIVolumeChannel.Master, value);
        }

        public void SetBgmVolume(float value)
        {
            SetVolume(UIVolumeChannel.Bgm, value);
        }

        public void SetSfxVolume(float value)
        {
            SetVolume(UIVolumeChannel.Sfx, value);
        }

        public void SetVoiceVolume(float value)
        {
            SetVolume(UIVolumeChannel.Voice, value);
        }

        public void SetUiVolume(float value)
        {
            SetVolume(UIVolumeChannel.Ui, value);
        }

        public float GetVolume(UIVolumeChannel channel)
        {
            switch (channel)
            {
                case UIVolumeChannel.Master:
                    return volumeSettings.master;
                case UIVolumeChannel.Bgm:
                    return volumeSettings.bgm;
                case UIVolumeChannel.Sfx:
                    return volumeSettings.sfx;
                case UIVolumeChannel.Ui:
                    return volumeSettings.ui;
                case UIVolumeChannel.Voice:
                    return volumeSettings.voice;
                default:
                    return 1f;
            }
        }

        public void SetVolume(UIVolumeChannel channel, float value)
        {
            value = Mathf.Clamp01(value);
            switch (channel)
            {
                case UIVolumeChannel.Master:
                    volumeSettings.master = value;
                    break;
                case UIVolumeChannel.Bgm:
                    volumeSettings.bgm = value;
                    break;
                case UIVolumeChannel.Sfx:
                    volumeSettings.sfx = value;
                    break;
                case UIVolumeChannel.Ui:
                    volumeSettings.ui = value;
                    break;
                case UIVolumeChannel.Voice:
                    volumeSettings.voice = value;
                    break;
            }

            ApplyVolumeSettings(true);
        }

        private void ApplyVolumeSettings(bool save)
        {
            AudioListener.volume = Mathf.Clamp01(volumeSettings.master);
            if (save)
            {
                Save();
            }

            RefreshStoryAudioAdapters();
            OnVolumeSettingsChanged?.Invoke(volumeSettings);
        }

        public static GameAudioChannel ToGameAudioChannel(UIVolumeChannel channel)
        {
            switch (channel)
            {
                case UIVolumeChannel.Master:
                    return GameAudioChannel.Master;
                case UIVolumeChannel.Bgm:
                    return GameAudioChannel.Bgm;
                case UIVolumeChannel.Sfx:
                    return GameAudioChannel.Sfx;
                case UIVolumeChannel.Ui:
                    return GameAudioChannel.Ui;
                case UIVolumeChannel.Voice:
                    return GameAudioChannel.Voice;
                default:
                    return GameAudioChannel.Master;
            }
        }

        private static void RefreshStoryAudioAdapters()
        {
            var adapters = FindObjectsOfType<StoryAudioAdapter>();
            for (var i = 0; i < adapters.Length; i++)
            {
                adapters[i].RefreshVolumeSettings();
            }
        }
    }

}
