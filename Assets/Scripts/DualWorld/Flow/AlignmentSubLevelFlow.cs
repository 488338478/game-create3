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

        public GameObject RealityRoot => realityRoot;
        public GameObject DreamRoot => dreamRoot;

        [Header("Traversal")]
        [SerializeField] private string exitWorkspaceEventId = "alignment_exit";
        [SerializeField] private string realityCompletedWorkspaceEventId = "reality.completed";

        [Header("Tuning")]
        [SerializeField] private float blockedToDreamDelaySec = 1f;

        protected override void OnInitialized()
        {
            if (realityTask != null) realityTask.SubmitAttempted += OnRealitySubmit;
            if (pushTarget != null) pushTarget.Completed += OnDreamComplete;
            if (Workspace != null) Workspace.WorkspaceEventRaised += HandleWorkspaceEvent;

            SubscribeToChatBox();
        }

        private void OnDestroy()
        {
            if (realityTask != null) realityTask.SubmitAttempted -= OnRealitySubmit;
            if (pushTarget != null) pushTarget.Completed -= OnDreamComplete;
            if (Workspace != null) Workspace.WorkspaceEventRaised -= HandleWorkspaceEvent;

            UnsubscribeFromChatBox();
        }

        // ── ChatBox abstract overrides ──

        protected override bool CanSubmit()
            => realityTask != null && realityTask.IsInteractable;

        protected override void DoSubmitRealityTask()
            => realityTask?.Submit();

        // ── Phase transitions ──

        protected override void OnPhaseEntered(SubLevelPhase phase)
        {
            switch (phase)
            {
                case SubLevelPhase.RealityTaskActive:
                    pushTarget?.ResetTarget();
                    realityTask?.ResetTask();
                    Workspace?.PlayerController?.SetInputEnabled(true);
                    PublishChatTask();
                    SetSubmitInteractable(true);
                    break;

                case SubLevelPhase.RealityTaskBlocked:
                    StartCoroutine(DelayThen(blockedToDreamDelaySec, () =>
                    {
                        if (CurrentPhase == SubLevelPhase.RealityTaskBlocked)
                            EnterPhase(SubLevelPhase.DreamTaskUnlocked);
                    }));
                    break;

                case SubLevelPhase.DreamTaskUnlocked:
                    Workspace?.EventBus.Raise(new CrossWorldEvent(CrossWorldEventType.DreamUnlocked, SubLevelId, null));
                    break;

                case SubLevelPhase.RealityTaskEnhanced:
                    break;

                case SubLevelPhase.RealityTaskCompleted:
                    realityTask?.SetInteractable(false);
                    SetSubmitInteractable(false);
                    Workspace?.RaiseWorkspaceEvent(realityCompletedWorkspaceEventId);
                    Workspace?.EventBus.Raise(new CrossWorldEvent(CrossWorldEventType.RealityCompleted, SubLevelId, null));
                    EnterPhase(SubLevelPhase.DreamWorldResolved);
                    break;

                case SubLevelPhase.DreamWorldResolved:
                    EnterPhase(SubLevelPhase.DreamTraversalActive);
                    break;

                case SubLevelPhase.DreamTraversalActive:
                    if (Workspace != null && Workspace.HasWorkspaceEvent(exitWorkspaceEventId))
                        EnterPhase(SubLevelPhase.SubLevelCompleted);
                    break;

                case SubLevelPhase.SubLevelCompleted:
                    Workspace?.EventBus.Raise(new CrossWorldEvent(CrossWorldEventType.ExitReached, SubLevelId, null));
                    break;
            }
        }

        public override void OnRealitySubmit(RealitySubmitResult result)
        {
            if (result.Success)
            {
                EnterPhase(SubLevelPhase.RealityTaskCompleted);
                return;
            }

            RaiseChatEvent(ChatTaskController.Event.Failed);

            if (CurrentPhase == SubLevelPhase.RealityTaskActive)
                EnterPhase(SubLevelPhase.RealityTaskBlocked);
        }

        public override void OnDreamComplete()
        {
            if ((int)CurrentPhase >= (int)SubLevelPhase.RealityTaskEnhanced) return;

            Workspace?.EventBus.Raise(new CrossWorldEvent(CrossWorldEventType.DreamCompleted, SubLevelId, null));
            EnterPhase(SubLevelPhase.RealityTaskEnhanced);
        }

        public override void OnTraversalReachedExit()
        {
            if ((int)CurrentPhase < (int)SubLevelPhase.RealityTaskCompleted) return;
            if (CurrentPhase == SubLevelPhase.SubLevelCompleted) return;
            EnterPhase(SubLevelPhase.SubLevelCompleted);
        }

        private void HandleWorkspaceEvent(string eventId)
        {
            if (eventId == exitWorkspaceEventId)
                OnTraversalReachedExit();
        }
    }
}
