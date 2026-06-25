using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace GameCreate3.StoryPlayer
{
    public sealed class StoryEventSystem : MonoBehaviour
    {
        [Header("Effects")]
        [SerializeField] private Transform effectContainer;

        private IAudioService audioService;
        private StoryVariableStore variableStore;
        private StoryPage currentPage;
        private CancellationTokenSource eventCts;
        private HashSet<int> triggeredEvents;
        private float pageStartTime;
        private bool isRunning;

        public static StoryEventSystem Active { get; private set; }

        public bool IsRunning => isRunning;

        public event Action<StoryPageEvent> OnEventTriggered;
        public event Action<string> OnDialogueRequested;
        public event Action<string> OnEffectRequested;
        public event Action OnVariableChanged;
        public event Action<string> OnPostProcessEffectRequested;

        private void Awake()
        {
            triggeredEvents = new HashSet<int>();
            isRunning = false;
            Active = this;
        }

        private void OnDestroy()
        {
            if (Active == this) Active = null;
            eventCts?.Cancel();
            eventCts?.Dispose();
            eventCts = null;
        }

        public void Initialize(IAudioService audio, StoryVariableStore store)
        {
            audioService = audio;
            variableStore = store;
        }

        public void StartEventTracking(StoryPage page)
        {
            eventCts?.Cancel();
            eventCts?.Dispose();
            ClearActiveEffects();

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
            ClearActiveEffects();
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
                // expected
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
                    HandlePlaySound(evt.EventData);
                    break;

                case StoryEventType.PlayMusic:
                    HandlePlayMusic(evt.EventData);
                    break;

                case StoryEventType.StopMusic:
                    audioService?.StopBgm();
                    break;

                case StoryEventType.TriggerEffect:
                    TriggerEffect(evt.EventData);
                    break;

                case StoryEventType.SetVariable:
                    SetVariable(evt.EventData);
                    break;

                case StoryEventType.Branch:
                    OnDialogueRequested?.Invoke(evt.EventData);
                    break;

                case StoryEventType.PostProcessEffect:
                    OnPostProcessEffectRequested?.Invoke(evt.EventData);
                    break;
            }
        }

        private void HandlePlaySound(string eventData)
        {
            if (string.IsNullOrEmpty(eventData) || audioService == null)
            {
                return;
            }

            var parts = eventData.Split('|');
            var soundName = parts[0];
            var volume = parts.Length > 1 && float.TryParse(parts[1], out var v) ? v : 1f;

            audioService.PlaySfx(soundName, volume);
        }

        private void HandlePlayMusic(string eventData)
        {
            if (string.IsNullOrEmpty(eventData) || audioService == null)
            {
                return;
            }

            var parts = eventData.Split('|');
            var musicName = parts[0];
            var volume = parts.Length > 1 && float.TryParse(parts[1], out var v) ? v : 1f;

            audioService.PlayBgm(musicName, volume);
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

        private void ClearActiveEffects()
        {
            if (effectContainer == null)
            {
                return;
            }

            for (int i = effectContainer.childCount - 1; i >= 0; i--)
            {
                var child = effectContainer.GetChild(i);
                if (child != null)
                {
                    Destroy(child.gameObject);
                }
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

            if (variableStore == null)
            {
                Debug.LogWarning("[StoryEventSystem] No variable store available.");
                return;
            }

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
    }
}
