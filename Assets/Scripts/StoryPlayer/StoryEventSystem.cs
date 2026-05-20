using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace GameCreate3.StoryPlayer
{
    public sealed class StoryEventSystem : MonoBehaviour
    {
        [Header("Audio")]
        [SerializeField] private AudioSource bgmSource;
        [SerializeField] private AudioSource sfxSource;
        [SerializeField] private AudioSource voiceSource;

        [Header("Effects")]
        [SerializeField] private Transform effectContainer;

        private StoryPage currentPage;
        private CancellationTokenSource eventCts;
        private HashSet<int> triggeredEvents;
        private float pageStartTime;
        private bool isRunning;

        public bool IsRunning => isRunning;

        public event Action<StoryPageEvent> OnEventTriggered;
        public event Action<string> OnDialogueRequested;
        public event Action<string> OnEffectRequested;
        public event Action OnVariableChanged;

        // 跨实例的全局事件 — StoryEventSystem 由 StoryPlayer 动态创建，
        // 用静态事件让外部服务（如 StrobeEffectController）无需绑定具体实例就能监听。
        public static event Action<string> OnPostProcessEffectRequested;

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
            triggeredEvents = new HashSet<int>();
            isRunning = false;
        }

        private void Cleanup()
        {
            eventCts?.Cancel();
            eventCts?.Dispose();
            eventCts = null;
        }

        public void StartEventTracking(StoryPage page)
        {
            Cleanup();

            currentPage = page;
            triggeredEvents.Clear();
            pageStartTime = Time.time;
            isRunning = true;

            eventCts = new CancellationTokenSource();

            _ = ProcessEventsAsync(eventCts.Token);
        }

        public void StopEventTracking()
        {
            eventCts?.Cancel();
            isRunning = false;
            currentPage = null;
        }

        public void ResetEvents()
        {
            triggeredEvents.Clear();
        }

        public void SkipToTime(float time)
        {
            if (currentPage == null)
            {
                return;
            }

            var events = currentPage.PageEvents;
            for (int i = 0; i < events.Count; i++)
            {
                var evt = events[i];
                if (evt.TriggerTime <= time && !triggeredEvents.Contains(i))
                {
                    TriggerEvent(evt, i);
                }
            }
        }

        private async Task ProcessEventsAsync(CancellationToken ct)
        {
            if (currentPage == null)
            {
                return;
            }

            try
            {
                var events = currentPage.PageEvents;
                var eventIndex = 0;

                while (eventIndex < events.Count && !ct.IsCancellationRequested)
                {
                    var evt = events[eventIndex];
                    var elapsedTime = Time.time - pageStartTime;
                    var waitTime = evt.TriggerTime - elapsedTime;

                    if (waitTime > 0)
                    {
                        await Task.Delay(TimeSpan.FromSeconds(waitTime), ct);
                    }

                    if (!ct.IsCancellationRequested && !triggeredEvents.Contains(eventIndex))
                    {
                        TriggerEvent(evt, eventIndex);
                    }

                    eventIndex++;
                }
            }
            catch (OperationCanceledException)
            {
                Debug.Log("[StoryEventSystem] Event processing cancelled.");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[StoryEventSystem] Event processing error: {ex.Message}");
            }
        }

        private void TriggerEvent(StoryPageEvent evt, int eventIndex)
        {
            triggeredEvents.Add(eventIndex);

            OnEventTriggered?.Invoke(evt);

            switch (evt.EventType)
            {
                case StoryEventType.PlaySound:
                    PlaySound(evt.EventData);
                    break;

                case StoryEventType.PlayMusic:
                    PlayMusic(evt.EventData);
                    break;

                case StoryEventType.StopMusic:
                    StopMusic();
                    break;

                case StoryEventType.TriggerEffect:
                    TriggerEffect(evt.EventData);
                    break;

                case StoryEventType.SetVariable:
                    SetVariable(evt.EventData);
                    break;

                case StoryEventType.Branch:
                    TriggerBranch(evt.EventData);
                    break;

                case StoryEventType.PostProcessEffect:
                    OnPostProcessEffectRequested?.Invoke(evt.EventData);
                    break;
            }
        }

        private void PlaySound(string eventData)
        {
            if (string.IsNullOrEmpty(eventData))
            {
                return;
            }

            var parts = eventData.Split('|');
            var soundName = parts[0];
            var volume = parts.Length > 1 && float.TryParse(parts[1], out var v) ? v : 1f;

            var clip = Resources.Load<AudioClip>($"Audio/SFX/{soundName}");
            if (clip != null && sfxSource != null)
            {
                sfxSource.PlayOneShot(clip, volume);
            }
            else
            {
                Debug.LogWarning($"[StoryEventSystem] Sound not found: {soundName}");
            }
        }

        private void PlayMusic(string eventData)
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
            if (clip != null && bgmSource != null)
            {
                bgmSource.clip = clip;
                bgmSource.volume = volume;
                bgmSource.loop = loop;
                bgmSource.Play();
            }
            else
            {
                Debug.LogWarning($"[StoryEventSystem] Music not found: {musicName}");
            }
        }

        private void StopMusic()
        {
            if (bgmSource != null)
            {
                bgmSource.Stop();
            }
        }

        private void TriggerEffect(string effectName)
        {
            if (string.IsNullOrEmpty(effectName))
            {
                return;
            }

            OnEffectRequested?.Invoke(effectName);

            var effectPrefab = Resources.Load<GameObject>($"Effects/{effectName}");
            if (effectPrefab != null && effectContainer != null)
            {
                Instantiate(effectPrefab, effectContainer);
            }
            else
            {
                Debug.LogWarning($"[StoryEventSystem] Effect not found: {effectName}");
            }
        }

        private void SetVariable(string eventData)
        {
            if (string.IsNullOrEmpty(eventData))
            {
                return;
            }

            var parts = eventData.Split('=');
            if (parts.Length != 2)
            {
                Debug.LogWarning($"[StoryEventSystem] Invalid variable format: {eventData}");
                return;
            }

            var key = parts[0].Trim();
            var value = parts[1].Trim();

            var variableStore = FindObjectOfType<StoryVariableStore>();
            if (variableStore != null)
            {
                if (bool.TryParse(value, out var boolValue))
                {
                    variableStore.SetBool(key, boolValue);
                }
                else if (int.TryParse(value, out var intValue))
                {
                    variableStore.SetInt(key, intValue);
                }
                else
                {
                    variableStore.SetString(key, value);
                }

                OnVariableChanged?.Invoke();
            }
        }

        private void TriggerBranch(string branchData)
        {
            if (string.IsNullOrEmpty(branchData))
            {
                return;
            }

            OnDialogueRequested?.Invoke(branchData);
        }

        public void TriggerManualEvent(StoryEventType eventType, string eventData)
        {
            var manualEvent = new StoryPageEvent
            {
                EventType = eventType,
                EventData = eventData,
                TriggerTime = 0f
            };

            TriggerEvent(manualEvent, -1);
        }

        public void PlayVoiceOver(AudioClip clip)
        {
            if (voiceSource != null && clip != null)
            {
                voiceSource.clip = clip;
                voiceSource.Play();
            }
        }

        public void StopVoiceOver()
        {
            if (voiceSource != null)
            {
                voiceSource.Stop();
            }
        }
    }
}
