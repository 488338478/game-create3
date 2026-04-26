using System.Collections;
using UnityEngine;

namespace GameCreate3.DualWorld
{
    public sealed class AlignmentSubLevelFlow : BaseSubLevelFlow
    {
        [Header("Reality")]
        [SerializeField] private RealityAlignmentTask realityTask;
        [SerializeField] private GameObject realityRoot;

        [Header("Dream")]
        [SerializeField] private DreamPushTarget pushTarget;
        [SerializeField] private GameObject dreamRoot;

        [Header("Traversal")]
        [SerializeField] private string exitWorkspaceEventId = "alignment_exit";

        [Header("Chat")]
        [SerializeField] private ChatTaskDefinition taskDefinition;

        [Header("Tuning")]
        [SerializeField] private float blockedToDreamDelaySec = 1f;

        protected override void OnInitialized()
        {
            if (realityTask != null) realityTask.SubmitAttempted += OnRealitySubmit;
            if (pushTarget != null) pushTarget.Completed += OnDreamComplete;
            if (Workspace != null) Workspace.WorkspaceEventRaised += HandleWorkspaceEvent;
        }

        private void OnDestroy()
        {
            if (realityTask != null) realityTask.SubmitAttempted -= OnRealitySubmit;
            if (pushTarget != null) pushTarget.Completed -= OnDreamComplete;
            if (Workspace != null) Workspace.WorkspaceEventRaised -= HandleWorkspaceEvent;
        }

        protected override void OnPhaseEntered(SubLevelPhase phase)
        {
            switch (phase)
            {
                case SubLevelPhase.RealityTaskActive:
                    SetRealityActive(true);
                    SetDreamActive(false);
                    realityTask?.ResetTask();
                    realityTask?.SetInteractable(true);
                    realityTask?.SetAssistEnabled(false);
                    Workspace?.PlayerController?.SetInputEnabled(false);
                    Workspace?.ChatTaskController?.Publish(taskDefinition);
                    break;

                case SubLevelPhase.RealityTaskBlocked:
                    realityTask?.SetInteractable(false);
                    Workspace?.EventBus.Raise(new CrossWorldEvent(CrossWorldEventType.RealityBlocked, SubLevelId, null));
                    StartCoroutine(DelayThen(blockedToDreamDelaySec, () => EnterPhase(SubLevelPhase.DreamTaskUnlocked)));
                    break;

                case SubLevelPhase.DreamTaskUnlocked:
                    SetRealityActive(false);
                    SetDreamActive(true);
                    pushTarget?.ResetTarget();
                    Workspace?.PlayerController?.SetInputEnabled(true);
                    Workspace?.EventBus.Raise(new CrossWorldEvent(CrossWorldEventType.DreamUnlocked, SubLevelId, null));
                    break;

                case SubLevelPhase.RealityTaskEnhanced:
                    SetRealityActive(true);
                    Workspace?.PlayerController?.SetInputEnabled(false);
                    break;

                case SubLevelPhase.RealityTaskCompleted:
                    realityTask?.SetInteractable(false);
                    Workspace?.EventBus.Raise(new CrossWorldEvent(CrossWorldEventType.RealityCompleted, SubLevelId, null));
                    break;

                case SubLevelPhase.DreamWorldResolved:
                    EnterPhase(SubLevelPhase.DreamTraversalActive);
                    break;

                case SubLevelPhase.DreamTraversalActive:
                    SetRealityActive(false);
                    SetDreamActive(true);
                    Workspace?.PlayerController?.SetInputEnabled(true);
                    break;

                case SubLevelPhase.SubLevelCompleted:
                    Workspace?.EventBus.Raise(new CrossWorldEvent(CrossWorldEventType.ExitReached, SubLevelId, null));
                    break;
            }
        }

        public override void OnRealitySubmit(RealitySubmitResult result)
        {
            if (result.Success && CurrentPhase == SubLevelPhase.RealityTaskEnhanced)
            {
                EnterPhase(SubLevelPhase.RealityTaskCompleted);
                return;
            }

            if (CurrentPhase == SubLevelPhase.RealityTaskActive)
            {
                Workspace?.ChatTaskController?.Raise(ChatTaskController.Event.Failed);
                EnterPhase(SubLevelPhase.RealityTaskBlocked);
            }
            else if (CurrentPhase == SubLevelPhase.RealityTaskEnhanced)
            {
                Workspace?.ChatTaskController?.Raise(ChatTaskController.Event.Failed);
            }
        }

        public override void OnDreamComplete()
        {
            if (CurrentPhase != SubLevelPhase.DreamTaskUnlocked) return;

            Workspace?.EventBus.Raise(new CrossWorldEvent(CrossWorldEventType.DreamCompleted, SubLevelId, null));
            EnterPhase(SubLevelPhase.RealityTaskEnhanced);
        }

        public override void OnTraversalReachedExit()
        {
            if (CurrentPhase != SubLevelPhase.DreamTraversalActive) return;
            EnterPhase(SubLevelPhase.SubLevelCompleted);
        }

        private void HandleWorkspaceEvent(string eventId)
        {
            if (eventId == exitWorkspaceEventId)
            {
                OnTraversalReachedExit();
            }
        }

        private void SetRealityActive(bool active)
        {
            if (realityRoot != null) realityRoot.SetActive(active);
        }

        private void SetDreamActive(bool active)
        {
            if (dreamRoot != null) dreamRoot.SetActive(active);
        }

        private IEnumerator DelayThen(float seconds, System.Action action)
        {
            yield return new WaitForSeconds(seconds);
            action?.Invoke();
        }
    }
}
