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

        // 暴露为只读属性 —— 双屏布局后两个 root 不再切 SetActive，
        // 但保留引用便于未来挂"高亮/降亮"视觉表现，也避免 CS0414 警告。
        public GameObject RealityRoot => realityRoot;
        public GameObject DreamRoot => dreamRoot;

        [Header("Traversal")]
        [SerializeField] private string exitWorkspaceEventId = "alignment_exit";
        [SerializeField] private string realityCompletedWorkspaceEventId = "reality.completed";

        [Header("Chat")]
        [SerializeField] private ChatTaskDefinition taskDefinition;

        [Header("Tuning")]
        [SerializeField] private float blockedToDreamDelaySec = 1f;
        // submit 后先出 player 语料，隔这点时间再触发评估（= NPC 返信），让两条消息错开。
        [SerializeField] private float submitToNpcReplyDelaySec = 0.35f;

        private ChatBoxUI chatBox;
        private Coroutine pendingSubmitRoutine;

        private ChatBoxUI EnsureChatBox()
        {
            if (chatBox == null) chatBox = Workspace?.ChatTaskController?.ChatBox ?? FindObjectOfType<ChatBoxUI>(true);
            return chatBox;
        }

        protected override void OnInitialized()
        {
            if (realityTask != null) realityTask.SubmitAttempted += OnRealitySubmit;
            if (pushTarget != null) pushTarget.Completed += OnDreamComplete;
            if (Workspace != null) Workspace.WorkspaceEventRaised += HandleWorkspaceEvent;

            if (EnsureChatBox() != null) chatBox.SubmitRequested += HandleSubmitRequested;
        }

        private void OnDestroy()
        {
            if (realityTask != null) realityTask.SubmitAttempted -= OnRealitySubmit;
            if (pushTarget != null) pushTarget.Completed -= OnDreamComplete;
            if (Workspace != null) Workspace.WorkspaceEventRaised -= HandleWorkspaceEvent;
            if (chatBox != null) chatBox.SubmitRequested -= HandleSubmitRequested;
        }

        private void HandleSubmitRequested()
        {
            if (realityTask == null || !realityTask.IsInteractable) return;

            // 先把玩家这条语料 append 出去。
            Workspace?.ChatTaskController?.AppendPlayerSubmit();

            // 评估（会顺带 append NPC 返信）错开一点时机再触发，避免和玩家消息同帧涌出。
            // 连点：若上一条倒计时还没结束又点了，重置倒计时——不管连点几次，
            // 都只在最后一次点击的延迟过后回一条 NPC。
            if (submitToNpcReplyDelaySec > 0f && isActiveAndEnabled)
            {
                if (pendingSubmitRoutine != null) StopCoroutine(pendingSubmitRoutine);
                pendingSubmitRoutine = StartCoroutine(DelayThen(submitToNpcReplyDelaySec, SubmitRealityTask));
            }
            else
            {
                SubmitRealityTask();
            }
        }

        private void SubmitRealityTask()
        {
            pendingSubmitRoutine = null;
            // 协程延迟期间状态可能变化（如任务被关闭），重新校验。
            if (realityTask == null || !realityTask.IsInteractable) return;
            realityTask.Submit();
        }

        protected override void OnPhaseEntered(SubLevelPhase phase)
        {
            // 设计原则：两侧（左 UI + 右横板）从头到尾都可操作。
            // 阶段不再代表"输入开关"，而是代表三件真实游戏状态：
            //   1) realityTask 是否还接受提交（仅 Completed 后关闭）
            //   2) realityTask 的 assist（吸附 + 严格容差） 开关
            //   3) 路径是否打开（由 RealityToDreamRepair 桥根据事件决定）
            switch (phase)
            {
                case SubLevelPhase.RealityTaskActive:
                    pushTarget?.ResetTarget();
                    realityTask?.ResetTask();          // ResetTask 内部 SetInteractable(true) + SetAssistEnabled(false)
                    Workspace?.PlayerController?.SetInputEnabled(true);
                    Workspace?.ChatTaskController?.Publish(taskDefinition);
                    if (EnsureChatBox() != null) chatBox.SetSubmitInteractable(true);
                    break;

                case SubLevelPhase.RealityTaskBlocked:
                    // 仅作为"卡关反馈"阶段标记；UI 不锁、玩家不锁，1 秒后自然推进。
                    StartCoroutine(DelayThen(blockedToDreamDelaySec, () =>
                    {
                        if (CurrentPhase == SubLevelPhase.RealityTaskBlocked)
                        {
                            EnterPhase(SubLevelPhase.DreamTaskUnlocked);
                        }
                    }));
                    break;

                case SubLevelPhase.DreamTaskUnlocked:
                    Workspace?.EventBus.Raise(new CrossWorldEvent(CrossWorldEventType.DreamUnlocked, SubLevelId, null));
                    break;

                case SubLevelPhase.RealityTaskEnhanced:
                    // assist 已由 DreamToRealityEnhancer 桥打开，这里只是事件标记。
                    break;

                case SubLevelPhase.RealityTaskCompleted:
                    realityTask?.SetInteractable(false);   // 任务完成，不再接受新提交
                    if (EnsureChatBox() != null) chatBox.SetSubmitInteractable(false);
                    Workspace?.RaiseWorkspaceEvent(realityCompletedWorkspaceEventId);
                    Workspace?.EventBus.Raise(new CrossWorldEvent(CrossWorldEventType.RealityCompleted, SubLevelId, null));
                    // RealityToDreamRepair 桥会在同一帧内打开路径并喊出 DreamWorldResolved 事件；
                    // 流自身也立刻推进到 Resolved → Traversal，避免依赖桥来驱动相位。
                    EnterPhase(SubLevelPhase.DreamWorldResolved);
                    break;

                case SubLevelPhase.DreamWorldResolved:
                    EnterPhase(SubLevelPhase.DreamTraversalActive);
                    break;

                case SubLevelPhase.DreamTraversalActive:
                    // 玩家全程都可走 —— 处理一种边界情况：玩家在 path 打开之前
                    // 已经跳过墙走进过出口区（事件已记到 Workspace.raisedEventIds），
                    // 此时直接判通过，免得用户被迫走出再走入。
                    if (Workspace != null && Workspace.HasWorkspaceEvent(exitWorkspaceEventId))
                    {
                        EnterPhase(SubLevelPhase.SubLevelCompleted);
                    }
                    break;

                case SubLevelPhase.SubLevelCompleted:
                    Workspace?.EventBus.Raise(new CrossWorldEvent(CrossWorldEventType.ExitReached, SubLevelId, null));
                    break;
            }
        }

        public override void OnRealitySubmit(RealitySubmitResult result)
        {
            // 新机制：每个 block 是按 observation 解锁 target → 拖入半径 → 自动吸附+锁定计数。
            // Submit 不再依赖 Enhanced 阶段；只要 RealityAlignmentTask 报 success（锁满）就算通过。
            if (result.Success)
            {
                EnterPhase(SubLevelPhase.RealityTaskCompleted);
                return;
            }

            Workspace?.ChatTaskController?.Raise(ChatTaskController.Event.Failed);

            // 仅"首次失败 + 还在 Active 阶段"时驱动 Blocked → DreamTaskUnlocked 反馈链。
            if (CurrentPhase == SubLevelPhase.RealityTaskActive)
            {
                EnterPhase(SubLevelPhase.RealityTaskBlocked);
            }
        }

        public override void OnDreamComplete()
        {
            // 玩家可能在任意时刻把方块推到舒适区（即便还没提交过）。
            // 只要还没进入 Enhanced 阶段，都接受这次"梦境完成"。
            if ((int)CurrentPhase >= (int)SubLevelPhase.RealityTaskEnhanced) return;

            Workspace?.EventBus.Raise(new CrossWorldEvent(CrossWorldEventType.DreamCompleted, SubLevelId, null));
            EnterPhase(SubLevelPhase.RealityTaskEnhanced);
        }

        public override void OnTraversalReachedExit()
        {
            // 出口只有在路径打开后（Completed 之后）才有意义；之前到达忽略。
            if ((int)CurrentPhase < (int)SubLevelPhase.RealityTaskCompleted) return;
            if (CurrentPhase == SubLevelPhase.SubLevelCompleted) return;
            EnterPhase(SubLevelPhase.SubLevelCompleted);
        }

        private void HandleWorkspaceEvent(string eventId)
        {
            if (eventId == exitWorkspaceEventId)
            {
                OnTraversalReachedExit();
            }
        }

        private IEnumerator DelayThen(float seconds, System.Action action)
        {
            yield return new WaitForSeconds(seconds);
            action?.Invoke();
        }
    }
}
