using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GameCreate3.DualWorld
{
    public sealed class ColorSubLevelFlow : BaseSubLevelFlow
    {
        [Header("Reality")]
        [SerializeField] private global::GameCreate3.ColorPuzzleController realityTask;
        [SerializeField] private GameObject realityRoot;
        [SerializeField] private string realityTaskId = "color.right";
        [SerializeField] private string realityCompletedWorkspaceEventId = "reality.color.completed";

        [Header("Dream")]
        [SerializeField] private global::GameCreate3.DreamColorCollectController dreamCollector;
        [SerializeField] private GameObject dreamRoot;

        [Header("Traversal")]
        [SerializeField] private bool autoCompleteAfterRealitySubmit = true;
        [SerializeField] private string exitWorkspaceEventId = "color_exit";

        [Header("Chat")]
        [SerializeField] private ChatTaskDefinition taskDefinition;

        [Header("Tuning")]
        [SerializeField] private float blockedToDreamDelaySec = 0.75f;
        [SerializeField] private bool dreamStartsUnlocked = true;

        public GameObject RealityRoot => realityRoot;
        public GameObject DreamRoot => dreamRoot;

        private readonly List<global::GameCreate3.PaletteColorOption> collectedDreamColors = new List<global::GameCreate3.PaletteColorOption>();

        private Coroutine pendingBlockedRoutine;
        private int realityFailCount;
        private float realityTaskStartTime;
        private global::GameCreate3.PaletteColorOption currentDreamPalette;

        protected override void OnInitialized()
        {
            ResolveRuntimeReferences();

            if (realityTask != null) realityTask.OnSubmitAttempted += HandleRealityTaskSubmit;
            if (dreamCollector != null) dreamCollector.Completed += HandleDreamCollectorCompleted;
            if (dreamCollector != null) dreamCollector.ItemCollected += HandleDreamCollectorItemCollected;
            if (Workspace != null) Workspace.WorkspaceEventRaised += HandleWorkspaceEvent;
        }

        private void OnDestroy()
        {
            CancelPendingBlockedTransition();

            if (realityTask != null) realityTask.OnSubmitAttempted -= HandleRealityTaskSubmit;
            if (dreamCollector != null) dreamCollector.Completed -= HandleDreamCollectorCompleted;
            if (dreamCollector != null) dreamCollector.ItemCollected -= HandleDreamCollectorItemCollected;
            if (Workspace != null) Workspace.WorkspaceEventRaised -= HandleWorkspaceEvent;
        }

        protected override void OnPhaseEntered(SubLevelPhase phase)
        {
            if (phase != SubLevelPhase.RealityTaskBlocked)
            {
                CancelPendingBlockedTransition();
            }

            switch (phase)
            {
                case SubLevelPhase.RealityTaskActive:
                    realityFailCount = 0;
                    realityTaskStartTime = Time.time;
                    currentDreamPalette = default;
                    collectedDreamColors.Clear();
                    realityTask?.ResetPuzzle();
                    realityTask?.ClearCurrentPalette();
                    realityTask?.SetDreamPaletteEnabled(false);
                    realityTask?.SetInteractable(true);
                    if (Workspace?.PlayerController != null)
                    {
                        dreamCollector?.SetPlayerTransform(Workspace.PlayerController.transform);
                    }
                    dreamCollector?.ResetStage();
                    dreamCollector?.SetInteractive(dreamStartsUnlocked);
                    realityTask?.SetDreamPaletteEnabled(dreamStartsUnlocked);
                    Workspace?.PlayerController?.SetInputEnabled(true);
                    if (dreamStartsUnlocked)
                    {
                        Workspace?.EventBus.Raise(new CrossWorldEvent(CrossWorldEventType.DreamUnlocked, SubLevelId, null));
                    }
                    if (taskDefinition != null)
                    {
                        Workspace?.ChatTaskController?.Publish(taskDefinition);
                    }
                    break;

                case SubLevelPhase.RealityTaskBlocked:
                    realityTask?.SetInteractable(false);
                    pendingBlockedRoutine = StartCoroutine(DelayThen(blockedToDreamDelaySec, () =>
                    {
                        if (CurrentPhase == SubLevelPhase.RealityTaskBlocked)
                        {
                            EnterPhase(SubLevelPhase.DreamTaskUnlocked);
                        }
                    }));
                    break;

                case SubLevelPhase.DreamTaskUnlocked:
                    realityTask?.SetDreamPaletteEnabled(true);
                    realityTask?.SetInteractable(true);
                    if (Workspace?.PlayerController != null)
                    {
                        dreamCollector?.SetPlayerTransform(Workspace.PlayerController.transform);
                    }
                    dreamCollector?.SetInteractive(true);
                    Workspace?.PlayerController?.SetInputEnabled(true);
                    Workspace?.EventBus.Raise(new CrossWorldEvent(CrossWorldEventType.DreamUnlocked, SubLevelId, null));
                    break;

                case SubLevelPhase.RealityTaskEnhanced:
                    dreamCollector?.SetInteractive(false);
                    realityTask?.SetDreamPaletteEnabled(true);
                    realityTask?.SetCurrentPalette(currentDreamPalette);
                    realityTask?.SetInteractable(true);
                    Workspace?.PlayerController?.SetInputEnabled(true);
                    break;

                case SubLevelPhase.RealityTaskCompleted:
                    realityTask?.SetInteractable(false);
                    dreamCollector?.SetInteractive(false);
                    Workspace?.RaiseWorkspaceEvent(realityCompletedWorkspaceEventId);
                    Workspace?.EventBus.Raise(new CrossWorldEvent(CrossWorldEventType.RealityCompleted, SubLevelId, collectedDreamColors.AsReadOnly()));

                    if (autoCompleteAfterRealitySubmit || string.IsNullOrWhiteSpace(exitWorkspaceEventId))
                    {
                        EnterPhase(SubLevelPhase.SubLevelCompleted);
                    }
                    else
                    {
                        EnterPhase(SubLevelPhase.DreamWorldResolved);
                    }
                    break;

                case SubLevelPhase.DreamWorldResolved:
                    EnterPhase(SubLevelPhase.DreamTraversalActive);
                    break;

                case SubLevelPhase.DreamTraversalActive:
                    Workspace?.PlayerController?.SetInputEnabled(true);
                    if (Workspace != null && Workspace.HasWorkspaceEvent(exitWorkspaceEventId))
                    {
                        EnterPhase(SubLevelPhase.SubLevelCompleted);
                    }
                    break;

                case SubLevelPhase.SubLevelCompleted:
                    Workspace?.EventBus.Raise(new CrossWorldEvent(CrossWorldEventType.ExitReached, SubLevelId, collectedDreamColors.AsReadOnly()));
                    break;
            }
        }

        public override void OnRealitySubmit(RealitySubmitResult result)
        {
            if (result.Success)
            {
                Workspace?.ChatTaskController?.Raise(ChatTaskController.Event.Completed);
                EnterPhase(SubLevelPhase.RealityTaskCompleted);
                return;
            }

            Workspace?.ChatTaskController?.Raise(ChatTaskController.Event.Failed);

            if (CurrentPhase == SubLevelPhase.RealityTaskActive && !dreamStartsUnlocked)
            {
                EnterPhase(SubLevelPhase.RealityTaskBlocked);
            }
        }

        public override void OnDreamComplete()
        {
            if ((int)CurrentPhase >= (int)SubLevelPhase.RealityTaskEnhanced)
            {
                return;
            }

            realityTask?.SetCurrentPalette(currentDreamPalette);
            Workspace?.EventBus.Raise(new CrossWorldEvent(CrossWorldEventType.DreamCompleted, SubLevelId, collectedDreamColors.AsReadOnly()));
            EnterPhase(SubLevelPhase.RealityTaskEnhanced);
        }

        public override void OnTraversalReachedExit()
        {
            if (autoCompleteAfterRealitySubmit || string.IsNullOrWhiteSpace(exitWorkspaceEventId))
            {
                return;
            }

            if ((int)CurrentPhase < (int)SubLevelPhase.RealityTaskCompleted)
            {
                return;
            }

            if (CurrentPhase == SubLevelPhase.SubLevelCompleted)
            {
                return;
            }

            EnterPhase(SubLevelPhase.SubLevelCompleted);
        }

        private void HandleRealityTaskSubmit(global::GameCreate3.ColorSubmitResult result)
        {
            if (!result.success)
            {
                realityFailCount++;
            }

            var submitResult = new RealitySubmitResult(
                realityTaskId,
                result.success,
                result.success ? null : result.feedbackLine,
                realityFailCount,
                Time.time - realityTaskStartTime);

            OnRealitySubmit(submitResult);
        }

        private void HandleDreamCollectorCompleted(IReadOnlyList<global::GameCreate3.PaletteColorOption> colors)
        {
            collectedDreamColors.Clear();
            if (colors != null)
            {
                for (var i = 0; i < colors.Count; i++)
                {
                    collectedDreamColors.Add(colors[i]);
                }
            }

            OnDreamComplete();
        }

        private void HandleDreamCollectorItemCollected(global::GameCreate3.PaletteColorOption option)
        {
            TrackCollectedDreamColor(option);
            currentDreamPalette = option;
            realityTask?.SetCurrentPalette(option);
            realityTask?.FlashTargetsForOption(option);
        }

        private void HandleWorkspaceEvent(string eventId)
        {
            if (eventId == exitWorkspaceEventId)
            {
                OnTraversalReachedExit();
            }
        }

        private void CancelPendingBlockedTransition()
        {
            if (pendingBlockedRoutine == null)
            {
                return;
            }

            StopCoroutine(pendingBlockedRoutine);
            pendingBlockedRoutine = null;
        }

        private IEnumerator DelayThen(float seconds, System.Action action)
        {
            if (seconds > 0f)
            {
                yield return new WaitForSeconds(seconds);
            }

            pendingBlockedRoutine = null;
            action?.Invoke();
        }

        private void ResolveRuntimeReferences()
        {
            if (realityTask == null && realityRoot != null)
            {
                realityTask = realityRoot.GetComponentInChildren<global::GameCreate3.ColorPuzzleController>(true);
                if (realityTask == null)
                {
                    realityTask = realityRoot.AddComponent<global::GameCreate3.ColorPuzzleController>();
                }
            }

            if (dreamCollector == null)
            {
                if (dreamRoot != null)
                {
                    dreamCollector = dreamRoot.GetComponentInChildren<global::GameCreate3.DreamColorCollectController>(true);
                }

                if (dreamCollector == null && realityRoot != null)
                {
                    dreamCollector = realityRoot.GetComponentInChildren<global::GameCreate3.DreamColorCollectController>(true);
                }
            }

            if (dreamCollector != null && Workspace?.PlayerController != null)
            {
                dreamCollector.SetPlayerTransform(Workspace.PlayerController.transform);
            }

            if (dreamCollector != null)
            {
                var meteorRoot = dreamRoot != null ? dreamRoot.transform : dreamCollector.transform;
                dreamCollector.SetMeteorContainer(meteorRoot);
            }
        }

        private void TrackCollectedDreamColor(global::GameCreate3.PaletteColorOption option)
        {
            if (!option.IsValid)
            {
                return;
            }

            for (var i = 0; i < collectedDreamColors.Count; i++)
            {
                if (collectedDreamColors[i].Matches(option))
                {
                    return;
                }
            }

            collectedDreamColors.Add(option);
        }
    }
}
