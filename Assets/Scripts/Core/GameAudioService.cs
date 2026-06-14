using System;
using System.Collections;
using System.Collections.Generic;
using GameCreate3.StoryPlayer;
using GameCreate3.UI;
using UnityEngine;

namespace GameCreate3.Core
{
    public enum GameAudioChannel
    {
        Master,
        Bgm,
        Ambient,
        Sfx,
        Ui,
        Voice
    }

    public sealed class GameAudioService : MonoBehaviour
    {
        private const string MasterVolumeKey = "Master_Volume";
        private const string BgmVolumeKey = "BGM_Volume";
        private const string SfxVolumeKey = "SFX_Volume";
        private const string VoiceVolumeKey = "Voice_Volume";
        private const string AmbientVolumeKey = "Ambient_Volume";
        private const string UiSfxVolumeKey = "UI_SFX_Volume";

        [SerializeField] private bool dontDestroyOnLoad = true;
        [SerializeField] private float defaultFadeSeconds = 1f;
        [SerializeField] private AnimationCurve fadeCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

        [Header("Default BGM")]
        [SerializeField] private bool playBgmOnAwake;
        [SerializeField] private AudioClip defaultBgmClip;
        [SerializeField] private bool defaultBgmLoop = true;
        [SerializeField] [Range(0f, 1f)] private float defaultBgmVolumeScale = 1f;

        [Header("Sources (optional — created if null)")]
        [SerializeField] private AudioSource bgmSource;
        [SerializeField] private AudioSource ambientSource;
        [SerializeField] private AudioSource sfxSource;
        [SerializeField] private AudioSource uiSource;
        [SerializeField] private AudioSource voiceSource;

        private readonly Dictionary<string, AudioClip> bgmCache = new Dictionary<string, AudioClip>(StringComparer.Ordinal);
        private readonly Dictionary<string, AudioClip> ambientCache = new Dictionary<string, AudioClip>(StringComparer.Ordinal);
        private readonly Dictionary<string, AudioClip> sfxCache = new Dictionary<string, AudioClip>(StringComparer.Ordinal);
        private Coroutine fadeRoutine;
        private float bgmContentScale = 1f;
        private float ambientContentScale = 1f;

        public static GameAudioService Instance { get; private set; }

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

            EnsureSources();
            ApplyStoredVolumesToSources();

            if (playBgmOnAwake && defaultBgmClip != null)
            {
                PlayBGM(defaultBgmClip, defaultBgmLoop, defaultBgmVolumeScale);
            }
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        public void PlayBGM(string bgmId, bool loop = true, float volumeScale = 1f)
        {
            PlayBGM(ResolveBgmClip(bgmId), loop, volumeScale);
        }

        public void PlayBGM(AudioClip clip, bool loop = true, float volumeScale = 1f)
        {
            if (clip == null || bgmSource == null)
            {
                return;
            }

            bgmContentScale = Mathf.Clamp01(volumeScale);
            bgmSource.clip = clip;
            bgmSource.loop = loop;
            bgmSource.volume = GetEffectiveBgmVolume() * bgmContentScale;
            bgmSource.Play();
        }

        public void StopBGM()
        {
            if (bgmSource != null)
            {
                bgmSource.Stop();
            }
        }

        public void PlayAmbient(string ambientId, bool loop = true, float volumeScale = 1f)
        {
            var clip = ResolveAmbientClip(ambientId);
            if (clip == null || ambientSource == null)
            {
                return;
            }

            ambientContentScale = Mathf.Clamp01(volumeScale);
            ambientSource.clip = clip;
            ambientSource.loop = loop;
            ambientSource.volume = GetEffectiveAmbientVolume() * ambientContentScale;
            ambientSource.Play();
        }

        public void StopAmbient()
        {
            if (ambientSource != null)
            {
                ambientSource.Stop();
            }
        }

        public void PlaySFX(string sfxId, GameAudioChannel channel = GameAudioChannel.Sfx, float volumeScale = 1f)
        {
            var clip = ResolveSfxClip(sfxId);
            if (clip == null)
            {
                return;
            }

            var src = channel == GameAudioChannel.Ui ? uiSource : sfxSource;
            if (src == null)
            {
                return;
            }

            var vol = channel == GameAudioChannel.Ui
                ? GetEffectiveUiVolume() * Mathf.Clamp01(volumeScale)
                : GetEffectiveSfxVolume() * Mathf.Clamp01(volumeScale);

            src.PlayOneShot(clip, vol);
        }

        public void SetVolume(GameAudioChannel channel, float value01)
        {
            value01 = Mathf.Clamp01(value01);
            switch (channel)
            {
                case GameAudioChannel.Master:
                    PlayerPrefs.SetFloat(MasterVolumeKey, value01);
                    break;
                case GameAudioChannel.Bgm:
                    PlayerPrefs.SetFloat(BgmVolumeKey, value01);
                    break;
                case GameAudioChannel.Ambient:
                    PlayerPrefs.SetFloat(AmbientVolumeKey, value01);
                    break;
                case GameAudioChannel.Sfx:
                    PlayerPrefs.SetFloat(SfxVolumeKey, value01);
                    break;
                case GameAudioChannel.Ui:
                    PlayerPrefs.SetFloat(UiSfxVolumeKey, value01);
                    break;
                case GameAudioChannel.Voice:
                    PlayerPrefs.SetFloat(VoiceVolumeKey, value01);
                    break;
            }

            PlayerPrefs.Save();
            ApplyStoredVolumesToSources();
            SyncUiSettingsServiceIfPresent(channel, value01);
            RefreshStoryAudioAdapters();
        }

        public float GetVolume(GameAudioChannel channel)
        {
            switch (channel)
            {
                case GameAudioChannel.Master:
                    return PlayerPrefs.GetFloat(MasterVolumeKey, 1f);
                case GameAudioChannel.Bgm:
                    return PlayerPrefs.GetFloat(BgmVolumeKey, 1f);
                case GameAudioChannel.Ambient:
                    return PlayerPrefs.GetFloat(AmbientVolumeKey, 1f);
                case GameAudioChannel.Sfx:
                    return PlayerPrefs.GetFloat(SfxVolumeKey, 1f);
                case GameAudioChannel.Ui:
                    return PlayerPrefs.GetFloat(UiSfxVolumeKey, PlayerPrefs.GetFloat(SfxVolumeKey, 1f));
                case GameAudioChannel.Voice:
                    return PlayerPrefs.GetFloat(VoiceVolumeKey, 1f);
                default:
                    return 1f;
            }
        }

        public void FadeIn(GameAudioChannel channel, float? durationSeconds = null)
        {
            StartFade(channel, true, durationSeconds ?? defaultFadeSeconds);
        }

        public void FadeOut(GameAudioChannel channel, float? durationSeconds = null)
        {
            StartFade(channel, false, durationSeconds ?? defaultFadeSeconds);
        }

        private void StartFade(GameAudioChannel channel, bool fadeIn, float duration)
        {
            if (fadeRoutine != null)
            {
                StopCoroutine(fadeRoutine);
                fadeRoutine = null;
            }

            fadeRoutine = StartCoroutine(FadeRoutine(channel, fadeIn, duration));
        }

        private IEnumerator FadeRoutine(GameAudioChannel channel, bool fadeIn, float duration)
        {
            if (channel == GameAudioChannel.Master)
            {
                var startM = AudioListener.volume;
                var endM = fadeIn ? Mathf.Clamp01(PlayerPrefs.GetFloat(MasterVolumeKey, 1f)) : 0f;
                var targetM = fadeIn ? endM : 0f;
                if (duration <= 0f)
                {
                    AudioListener.volume = targetM;
                }
                else
                {
                    var elapsed = 0f;
                    while (elapsed < duration)
                    {
                        elapsed += Time.unscaledDeltaTime;
                        var t = fadeCurve.Evaluate(Mathf.Clamp01(elapsed / duration));
                        AudioListener.volume = Mathf.Lerp(startM, targetM, t);
                        yield return null;
                    }

                    AudioListener.volume = targetM;
                }

                fadeRoutine = null;
                yield break;
            }

            var source = GetDominantSourceForChannel(channel);
            if (source == null || duration <= 0f)
            {
                if (source != null)
                {
                    source.volume = fadeIn ? GetTargetVolumeForSource(channel, source) : 0f;
                }

                fadeRoutine = null;
                yield break;
            }

            var end = GetTargetVolumeForSource(channel, source);
            var start = fadeIn ? 0f : source.volume;
            var target = fadeIn ? end : 0f;
            var elapsed2 = 0f;

            while (elapsed2 < duration)
            {
                elapsed2 += Time.unscaledDeltaTime;
                var t = fadeCurve.Evaluate(Mathf.Clamp01(elapsed2 / duration));
                source.volume = Mathf.Lerp(start, target, t);
                yield return null;
            }

            source.volume = target;
            fadeRoutine = null;
        }

        private AudioSource GetDominantSourceForChannel(GameAudioChannel channel)
        {
            switch (channel)
            {
                case GameAudioChannel.Bgm:
                    return bgmSource;
                case GameAudioChannel.Ambient:
                    return ambientSource;
                case GameAudioChannel.Sfx:
                    return sfxSource;
                case GameAudioChannel.Ui:
                    return uiSource;
                case GameAudioChannel.Voice:
                    return voiceSource;
                default:
                    return null;
            }
        }

        private float GetTargetVolumeForSource(GameAudioChannel channel, AudioSource source)
        {
            if (source == bgmSource)
            {
                return GetEffectiveBgmVolume() * bgmContentScale;
            }

            if (source == ambientSource)
            {
                return GetEffectiveAmbientVolume() * ambientContentScale;
            }

            if (source == voiceSource)
            {
                return GetEffectiveVoiceVolume();
            }

            return source.volume;
        }

        private AudioClip ResolveBgmClip(string bgmId)
        {
            if (string.IsNullOrEmpty(bgmId))
            {
                return null;
            }

            if (bgmCache.TryGetValue(bgmId, out var cached))
            {
                return cached;
            }

            var clip = Resources.Load<AudioClip>($"Audio/BGM/{bgmId}");
            if (clip != null)
            {
                bgmCache[bgmId] = clip;
            }

            return clip;
        }

        private AudioClip ResolveSfxClip(string sfxId)
        {
            if (string.IsNullOrEmpty(sfxId))
            {
                return null;
            }

            if (sfxCache.TryGetValue(sfxId, out var cached))
            {
                return cached;
            }

            var clip = Resources.Load<AudioClip>($"Audio/SFX/{sfxId}");
            if (clip != null)
            {
                sfxCache[sfxId] = clip;
            }

            return clip;
        }

        private AudioClip ResolveAmbientClip(string ambientId)
        {
            if (string.IsNullOrEmpty(ambientId))
            {
                return null;
            }

            if (ambientCache.TryGetValue(ambientId, out var cached))
            {
                return cached;
            }

            var clip = Resources.Load<AudioClip>($"Audio/Ambient/{ambientId}");
            if (clip != null)
            {
                ambientCache[ambientId] = clip;
            }

            return clip;
        }

        private void EnsureSources()
        {
            bgmSource = EnsureLoopingSource(bgmSource, "Core_BGM");
            ambientSource = EnsureLoopingSource(ambientSource, "Core_Ambient");
            sfxSource = EnsureOneShotSource(sfxSource, "Core_SFX");
            uiSource = EnsureOneShotSource(uiSource, "Core_UI");
            voiceSource = EnsureOneShotSource(voiceSource, "Core_Voice");
        }

        private AudioSource EnsureLoopingSource(AudioSource existing, string childName)
        {
            if (existing != null)
            {
                existing.loop = true;
                existing.playOnAwake = false;
                return existing;
            }

            var go = new GameObject(childName);
            var src = go.AddComponent<AudioSource>();
            src.loop = true;
            src.playOnAwake = false;
            go.transform.SetParent(transform, false);
            return src;
        }

        private AudioSource EnsureOneShotSource(AudioSource existing, string childName)
        {
            if (existing != null)
            {
                existing.playOnAwake = false;
                return existing;
            }

            var go = new GameObject(childName);
            var src = go.AddComponent<AudioSource>();
            src.playOnAwake = false;
            go.transform.SetParent(transform, false);
            return src;
        }

        private float GetEffectiveBgmVolume()
        {
            return Mathf.Clamp01(PlayerPrefs.GetFloat(MasterVolumeKey, 1f)) *
                   Mathf.Clamp01(PlayerPrefs.GetFloat(BgmVolumeKey, 1f));
        }

        private float GetEffectiveAmbientVolume()
        {
            return Mathf.Clamp01(PlayerPrefs.GetFloat(MasterVolumeKey, 1f)) *
                   Mathf.Clamp01(PlayerPrefs.GetFloat(AmbientVolumeKey, 1f));
        }

        private float GetEffectiveSfxVolume()
        {
            return Mathf.Clamp01(PlayerPrefs.GetFloat(MasterVolumeKey, 1f)) *
                   Mathf.Clamp01(PlayerPrefs.GetFloat(SfxVolumeKey, 1f));
        }

        private float GetEffectiveUiVolume()
        {
            return Mathf.Clamp01(PlayerPrefs.GetFloat(MasterVolumeKey, 1f)) *
                   Mathf.Clamp01(PlayerPrefs.GetFloat(UiSfxVolumeKey, PlayerPrefs.GetFloat(SfxVolumeKey, 1f)));
        }

        private float GetEffectiveVoiceVolume()
        {
            return Mathf.Clamp01(PlayerPrefs.GetFloat(MasterVolumeKey, 1f)) *
                   Mathf.Clamp01(PlayerPrefs.GetFloat(VoiceVolumeKey, 1f));
        }

        private void ApplyStoredVolumesToSources()
        {
            AudioListener.volume = Mathf.Clamp01(PlayerPrefs.GetFloat(MasterVolumeKey, 1f));
            if (bgmSource != null && bgmSource.isPlaying)
            {
                bgmSource.volume = GetEffectiveBgmVolume() * bgmContentScale;
            }

            if (ambientSource != null && ambientSource.isPlaying)
            {
                ambientSource.volume = GetEffectiveAmbientVolume() * ambientContentScale;
            }

            if (voiceSource != null && voiceSource.clip != null)
            {
                voiceSource.volume = GetEffectiveVoiceVolume();
            }
        }

        private static void SyncUiSettingsServiceIfPresent(GameAudioChannel channel, float value01)
        {
            var svc = UISettingsService.Instance;
            if (svc == null)
            {
                return;
            }

            switch (channel)
            {
                case GameAudioChannel.Master:
                    svc.SetMasterVolume(value01);
                    break;
                case GameAudioChannel.Bgm:
                    svc.SetBgmVolume(value01);
                    break;
                case GameAudioChannel.Sfx:
                    svc.SetSfxVolume(value01);
                    break;
                case GameAudioChannel.Voice:
                    svc.SetVoiceVolume(value01);
                    break;
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
