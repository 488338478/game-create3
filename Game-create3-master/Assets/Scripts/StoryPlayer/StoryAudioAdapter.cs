using System;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace GameCreate3.StoryPlayer
{
    public sealed class StoryAudioAdapter : MonoBehaviour
    {
        [Header("Audio Sources")]
        [SerializeField] private AudioSource bgmSource;
        [SerializeField] private AudioSource sfxSource;
        [SerializeField] private AudioSource voiceSource;

        [Header("Settings")]
        [SerializeField] private float bgmFadeDuration = 1f;
        [SerializeField] private float crossFadeDuration = 1.5f;
        [SerializeField] private AnimationCurve fadeCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

        private StoryEventSystem eventSystem;
        private AudioClip currentBgm;
        private AudioClip pendingBgm;
        private CancellationTokenSource fadeCts;
        private float baseBgmVolume = 1f;
        private float baseSfxVolume = 1f;
        private float baseVoiceVolume = 1f;

        public bool IsBgmPlaying => bgmSource != null && bgmSource.isPlaying;
        public AudioClip CurrentBgm => currentBgm;

        public event Action OnBgmChanged;
        public event Action OnSfxPlayed;

        private void Awake()
        {
            Initialize();
        }

        private void OnDestroy()
        {
            Cleanup();
        }

        private void Initialize()
        {
            if (bgmSource == null)
            {
                bgmSource = gameObject.AddComponent<AudioSource>();
                bgmSource.loop = true;
                bgmSource.playOnAwake = false;
            }

            if (sfxSource == null)
            {
                sfxSource = gameObject.AddComponent<AudioSource>();
                sfxSource.playOnAwake = false;
            }

            if (voiceSource == null)
            {
                voiceSource = gameObject.AddComponent<AudioSource>();
                voiceSource.playOnAwake = false;
            }

            LoadVolumeSettings();
        }

        private void Cleanup()
        {
            fadeCts?.Cancel();
            fadeCts?.Dispose();
            fadeCts = null;
        }

        public void BindEventSystem(StoryEventSystem eventSystem)
        {
            if (this.eventSystem != null)
            {
                this.eventSystem.OnEventTriggered -= HandleStoryEvent;
            }

            this.eventSystem = eventSystem;

            if (eventSystem != null)
            {
                eventSystem.OnEventTriggered += HandleStoryEvent;
            }
        }

        private void OnDisable()
        {
            if (eventSystem != null)
            {
                eventSystem.OnEventTriggered -= HandleStoryEvent;
            }
        }

        private void HandleStoryEvent(StoryPageEvent evt)
        {
            switch (evt.EventType)
            {
                case StoryEventType.PlaySound:
                    PlaySfxFromEvent(evt.EventData);
                    break;

                case StoryEventType.PlayMusic:
                    PlayBgmFromEvent(evt.EventData);
                    break;

                case StoryEventType.StopMusic:
                    StopBgm();
                    break;
            }
        }

        public void ApplyPageAudioConfig(StoryAudioConfig config)
        {
            if (config == null)
            {
                return;
            }

            if (config.Bgm != null && config.Bgm != currentBgm)
            {
                _ = CrossFadeBgmAsync(config.Bgm, config.BgmVolume, config.LoopBgm);
            }

            if (config.VoiceOver != null)
            {
                PlayVoiceOver(config.VoiceOver);
            }

            foreach (var sfx in config.SoundEffects)
            {
                _ = ScheduleSfxAsync(sfx);
            }
        }

        public async Task PlayBgmAsync(AudioClip clip, float volume = 1f, bool loop = true, bool fadeIn = true)
        {
            if (clip == null || bgmSource == null)
            {
                return;
            }

            fadeCts?.Cancel();
            fadeCts?.Dispose();
            fadeCts = new CancellationTokenSource();

            pendingBgm = clip;
            baseBgmVolume = volume;

            var targetVolume = GetGlobalBgmVolume() * volume;

            if (fadeIn && bgmSource.isPlaying)
            {
                await FadeOutAsync(bgmSource, bgmFadeDuration, fadeCts.Token);
            }

            currentBgm = clip;
            bgmSource.clip = clip;
            bgmSource.loop = loop;
            bgmSource.volume = fadeIn ? 0f : targetVolume;
            bgmSource.Play();

            if (fadeIn)
            {
                await FadeInAsync(bgmSource, targetVolume, bgmFadeDuration, fadeCts.Token);
            }

            OnBgmChanged?.Invoke();
        }

        public async Task CrossFadeBgmAsync(AudioClip newClip, float volume = 1f, bool loop = true)
        {
            if (newClip == null || bgmSource == null)
            {
                return;
            }

            if (newClip == currentBgm)
            {
                return;
            }

            fadeCts?.Cancel();
            fadeCts?.Dispose();
            fadeCts = new CancellationTokenSource();

            pendingBgm = newClip;
            baseBgmVolume = volume;

            var targetVolume = GetGlobalBgmVolume() * volume;

            if (bgmSource.isPlaying)
            {
                await CrossFadeAsync(newClip, targetVolume, loop, crossFadeDuration, fadeCts.Token);
            }
            else
            {
                await PlayBgmAsync(newClip, volume, loop, true);
            }
        }

        public async Task StopBgmAsync(bool fadeOut = true)
        {
            if (bgmSource == null || !bgmSource.isPlaying)
            {
                return;
            }

            fadeCts?.Cancel();
            fadeCts?.Dispose();
            fadeCts = new CancellationTokenSource();

            if (fadeOut)
            {
                await FadeOutAsync(bgmSource, bgmFadeDuration, fadeCts.Token);
            }

            bgmSource.Stop();
            currentBgm = null;
            pendingBgm = null;
        }

        public void StopBgm()
        {
            _ = StopBgmAsync(true);
        }

        public void PlaySfx(AudioClip clip, float volume = 1f)
        {
            if (clip == null || sfxSource == null)
            {
                return;
            }

            var finalVolume = GetGlobalSfxVolume() * volume;
            sfxSource.PlayOneShot(clip, finalVolume);
            OnSfxPlayed?.Invoke();
        }

        public void PlaySfx(string clipName, float volume = 1f)
        {
            var clip = Resources.Load<AudioClip>($"Audio/SFX/{clipName}");
            if (clip != null)
            {
                PlaySfx(clip, volume);
            }
            else
            {
                Debug.LogWarning($"[StoryAudioAdapter] SFX not found: {clipName}");
            }
        }

        public void PlayVoiceOver(AudioClip clip)
        {
            if (clip == null || voiceSource == null)
            {
                return;
            }

            voiceSource.clip = clip;
            voiceSource.volume = GetGlobalVoiceVolume();
            voiceSource.Play();
        }

        public void StopVoiceOver()
        {
            if (voiceSource != null)
            {
                voiceSource.Stop();
            }
        }

        public void SetBgmVolumeMultiplier(float multiplier)
        {
            baseBgmVolume = multiplier;
            UpdateBgmVolume();
        }

        public void PauseBgm()
        {
            bgmSource?.Pause();
        }

        public void ResumeBgm()
        {
            bgmSource?.UnPause();
        }

        private async Task ScheduleSfxAsync(StorySoundEffect sfx)
        {
            if (sfx.Clip == null)
            {
                return;
            }

            await Task.Delay(TimeSpan.FromSeconds(sfx.TriggerTime));

            if (this == null)
            {
                return;
            }

            PlaySfx(sfx.Clip, sfx.Volume);
        }

        private void PlaySfxFromEvent(string eventData)
        {
            if (string.IsNullOrEmpty(eventData))
            {
                return;
            }

            var parts = eventData.Split('|');
            var soundName = parts[0];
            var volume = parts.Length > 1 && float.TryParse(parts[1], out var v) ? v : 1f;

            PlaySfx(soundName, volume);
        }

        private void PlayBgmFromEvent(string eventData)
        {
            if (string.IsNullOrEmpty(eventData))
            {
                return;
            }

            var parts = eventData.Split('|');
            var musicName = parts[0];
            var volume = parts.Length > 1 && float.TryParse(parts[1], out var v) ? v : 1f;
            var loop = parts.Length <= 2 || !bool.TryParse(parts[2], out var l) || l;

            var clip = Resources.Load<AudioClip>($"Audio/BGM/{musicName}");
            if (clip != null)
            {
                _ = PlayBgmAsync(clip, volume, loop, true);
            }
            else
            {
                Debug.LogWarning($"[StoryAudioAdapter] BGM not found: {musicName}");
            }
        }

        private async Task CrossFadeAsync(AudioClip newClip, float targetVolume, bool loop, float duration, CancellationToken ct)
        {
            var tempSource = gameObject.AddComponent<AudioSource>();
            tempSource.clip = newClip;
            tempSource.loop = loop;
            tempSource.volume = 0f;
            tempSource.Play();

            var oldSource = bgmSource;
            bgmSource = tempSource;
            currentBgm = newClip;

            var elapsed = 0f;
            var oldVolume = oldSource.volume;

            while (elapsed < duration && !ct.IsCancellationRequested)
            {
                elapsed += Time.deltaTime;
                var t = fadeCurve.Evaluate(Mathf.Clamp01(elapsed / duration));

                oldSource.volume = Mathf.Lerp(oldVolume, 0f, t);
                tempSource.volume = Mathf.Lerp(0f, targetVolume, t);

                await Task.Yield();
            }

            oldSource.Stop();
            Destroy(oldSource);

            OnBgmChanged?.Invoke();
        }

        private async Task FadeInAsync(AudioSource source, float targetVolume, float duration, CancellationToken ct)
        {
            var elapsed = 0f;

            while (elapsed < duration && !ct.IsCancellationRequested)
            {
                elapsed += Time.deltaTime;
                var t = fadeCurve.Evaluate(Mathf.Clamp01(elapsed / duration));
                source.volume = Mathf.Lerp(0f, targetVolume, t);
                await Task.Yield();
            }

            source.volume = targetVolume;
        }

        private async Task FadeOutAsync(AudioSource source, float duration, CancellationToken ct)
        {
            var elapsed = 0f;
            var startVolume = source.volume;

            while (elapsed < duration && !ct.IsCancellationRequested)
            {
                elapsed += Time.deltaTime;
                var t = fadeCurve.Evaluate(Mathf.Clamp01(elapsed / duration));
                source.volume = Mathf.Lerp(startVolume, 0f, t);
                await Task.Yield();
            }

            source.volume = 0f;
        }

        private void UpdateBgmVolume()
        {
            if (bgmSource != null)
            {
                bgmSource.volume = GetGlobalBgmVolume() * baseBgmVolume;
            }
        }

        private float GetGlobalBgmVolume()
        {
            return PlayerPrefs.GetFloat("BGM_Volume", 1f);
        }

        private float GetGlobalSfxVolume()
        {
            return PlayerPrefs.GetFloat("SFX_Volume", 1f);
        }

        private float GetGlobalVoiceVolume()
        {
            return PlayerPrefs.GetFloat("Voice_Volume", 1f);
        }

        private void LoadVolumeSettings()
        {
            UpdateBgmVolume();
        }

        public void RefreshVolumeSettings()
        {
            LoadVolumeSettings();
            UpdateBgmVolume();
        }
    }
}
