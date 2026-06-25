using System;
using UnityEngine;
using GameCreate3.Core.SceneRouting;

namespace GameCreate3.StoryPlayer
{
    public sealed class StoryFlowBridge : MonoBehaviour
    {
        [Header("Flow Controller")]
        [SerializeField] private FlowController flowControllerRef;

        [Header("Settings")]
        [SerializeField] private bool autoBindToStoryPlayer = true;
        [SerializeField] private string defaultLevelSceneName = "Level_01";
        [SerializeField] private string mainMenuSceneName = "MainMenu";
        [SerializeField] private string gallerySceneName = "CGGallery";

        private IFlowController flowController;
        private StoryPlayer storyPlayer;

        public event Action<string> OnLevelEnterRequested;
        public event Action<string> OnSideScrollerEnterRequested;
        public event Action OnMainMenuRequested;
        public event Action<string> OnGalleryOpenRequested;
        public event Action<string> OnCGUnlockRequested;
        public event Action<string> OnCustomEventTriggered;

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
            if (flowController == null && flowControllerRef != null)
            {
                flowController = flowControllerRef;
            }

            if (autoBindToStoryPlayer)
            {
                storyPlayer = GetComponentInParent<StoryPlayer>();
                BindStoryPlayer(storyPlayer);
            }
        }

        public void SetFlowController(IFlowController controller)
        {
            flowController = controller;
        }

        private void Cleanup()
        {
            if (storyPlayer != null)
            {
                storyPlayer.OnSequenceCompleted -= HandleSequenceCompleted;
                storyPlayer.OnSequenceSkipped -= HandleSequenceSkipped;
            }
        }

        public void BindStoryPlayer(StoryPlayer player)
        {
            if (storyPlayer != null)
            {
                storyPlayer.OnSequenceCompleted -= HandleSequenceCompleted;
                storyPlayer.OnSequenceSkipped -= HandleSequenceSkipped;
            }

            storyPlayer = player;

            if (storyPlayer != null)
            {
                storyPlayer.OnSequenceCompleted += HandleSequenceCompleted;
                storyPlayer.OnSequenceSkipped += HandleSequenceSkipped;
            }
        }

        private void HandleSequenceCompleted()
        {
            ProcessEndCallback(storyPlayer?.CurrentSequence);
        }

        private void HandleSequenceSkipped()
        {
            ProcessEndCallback(storyPlayer?.CurrentSequence);
        }

        private void ProcessEndCallback(StorySequence sequence)
        {
            if (sequence == null)
            {
                return;
            }

            var callbackType = sequence.EndCallbackType;
            var callbackParam = sequence.EndCallbackParameter;

            switch (callbackType)
            {
                case StoryEndCallbackType.None:
                    break;

                case StoryEndCallbackType.EnterLevel:
                    EnterLevel(string.IsNullOrEmpty(callbackParam) ? defaultLevelSceneName : callbackParam);
                    break;

                case StoryEndCallbackType.EnterSideScroller:
                    EnterSideScroller(callbackParam);
                    break;

                case StoryEndCallbackType.EnterMainMenu:
                    EnterMainMenu();
                    break;

                case StoryEndCallbackType.EnterDialogue:
                    EnterDialogue(callbackParam);
                    break;

                case StoryEndCallbackType.TriggerEvent:
                    TriggerCustomEvent(callbackParam);
                    break;
            }
        }

        public void EnterLevel(string levelSceneName)
        {
            if (string.IsNullOrEmpty(levelSceneName))
            {
                levelSceneName = defaultLevelSceneName;
            }

            OnLevelEnterRequested?.Invoke(levelSceneName);

            if (flowController != null)
            {
                flowController.EnterLevel(levelSceneName);
            }
            else
            {
                LoadScene(levelSceneName);
            }
        }

        public void EnterSideScroller(string sideScrollerId)
        {
            OnSideScrollerEnterRequested?.Invoke(sideScrollerId);

            if (flowController != null)
            {
                flowController.EnterSideScroller(sideScrollerId);
            }
            else
            {
                Debug.Log($"[StoryFlowBridge] Entering SideScroller: {sideScrollerId}");
            }
        }

        public void EnterMainMenu()
        {
            OnMainMenuRequested?.Invoke();

            if (flowController != null)
            {
                flowController.EnterMainMenu();
            }
            else
            {
                LoadScene(mainMenuSceneName);
            }
        }

        public void EnterDialogue(string dialogueId)
        {
            if (flowController != null)
            {
                flowController.EnterDialogue(dialogueId);
            }
            else
            {
                Debug.Log($"[StoryFlowBridge] Entering Dialogue: {dialogueId}");
            }
        }

        public void OpenCGGallery(string galleryId = null)
        {
            OnGalleryOpenRequested?.Invoke(galleryId);

            if (flowController != null)
            {
                flowController.OpenCGGallery(galleryId);
            }
            else
            {
                LoadScene(gallerySceneName);
            }
        }

        public void UnlockCG(string cgId)
        {
            if (string.IsNullOrEmpty(cgId))
            {
                return;
            }

            OnCGUnlockRequested?.Invoke(cgId);

            PlayerPrefs.SetInt($"CG_Unlocked_{cgId}", 1);
            PlayerPrefs.Save();

            if (flowController != null)
            {
                flowController.UnlockCG(cgId);
            }
            else
            {
                Debug.Log($"[StoryFlowBridge] CG Unlocked: {cgId}");
            }
        }

        public void TriggerCustomEvent(string eventName)
        {
            if (string.IsNullOrEmpty(eventName))
            {
                return;
            }

            OnCustomEventTriggered?.Invoke(eventName);

            if (flowController != null)
            {
                flowController.TriggerEvent(eventName);
            }
            else
            {
                Debug.Log($"[StoryFlowBridge] Custom Event Triggered: {eventName}");
            }
        }

        public void ReturnToPreviousState()
        {
            if (flowController != null)
            {
                flowController.ReturnToPreviousState();
            }
            else
            {
                Debug.Log("[StoryFlowBridge] Returning to previous state");
            }
        }

        private void LoadScene(string sceneName)
        {
            if (string.IsNullOrEmpty(sceneName))
            {
                Debug.LogWarning("[StoryFlowBridge] Scene name is empty.");
                return;
            }

            SceneRouter.GoScene(sceneName);
        }
    }

    public interface IFlowController
    {
        void EnterLevel(string levelSceneName);
        void EnterSideScroller(string sideScrollerId);
        void EnterMainMenu();
        void EnterDialogue(string dialogueId);
        void OpenCGGallery(string galleryId);
        void UnlockCG(string cgId);
        void TriggerEvent(string eventName);
        void ReturnToPreviousState();
    }

    public class FlowController : MonoBehaviour, IFlowController
    {
        [SerializeField] private string currentState;
        [SerializeField] private string previousState;

        public void EnterLevel(string levelSceneName)
        {
            SaveState();
            currentState = $"Level_{levelSceneName}";
            SceneRouter.GoScene(levelSceneName);
        }

        public void EnterSideScroller(string sideScrollerId)
        {
            SaveState();
            currentState = $"SideScroller_{sideScrollerId}";
            Debug.Log($"[FlowController] Entering SideScroller: {sideScrollerId}");
        }

        public void EnterMainMenu()
        {
            SaveState();
            currentState = "MainMenu";
            SceneRouter.Go("main_menu");
        }

        public void EnterDialogue(string dialogueId)
        {
            SaveState();
            currentState = $"Dialogue_{dialogueId}";
            Debug.Log($"[FlowController] Entering Dialogue: {dialogueId}");
        }

        public void OpenCGGallery(string galleryId)
        {
            SaveState();
            currentState = "CGGallery";
            SceneRouter.GoScene("CGGallery");
        }

        public void UnlockCG(string cgId)
        {
            Debug.Log($"[FlowController] CG Unlocked: {cgId}");
        }

        public void TriggerEvent(string eventName)
        {
            Debug.Log($"[FlowController] Event Triggered: {eventName}");
        }

        public void ReturnToPreviousState()
        {
            if (!string.IsNullOrEmpty(previousState))
            {
                Debug.Log($"[FlowController] Returning to: {previousState}");
            }
        }

        private void SaveState()
        {
            previousState = currentState;
        }
    }
}
