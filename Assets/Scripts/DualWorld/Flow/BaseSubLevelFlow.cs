using System;
using System.Collections;
using GameCreate3.Core.SceneRouting;
using UnityEngine;

namespace GameCreate3.DualWorld
{
    public abstract class BaseSubLevelFlow : MonoBehaviour
    {
        [SerializeField] private string subLevelId = "sublevel";

        [Header("Chat")]
        [SerializeField] private ChatTaskDefinition taskDefinition;

        [Header("Submit Tuning")]
        [SerializeField] private float submitToNpcReplyDelaySec = 0.35f;

        [Header("Scene Transition")]
        [SerializeField] private float successTransitionDelaySec = 1.1f;
        [SerializeField] private string successSceneName = "";

        public string SubLevelId => subLevelId;
        public SubLevelPhase CurrentPhase { get; private set; }
        public DualWorldWorkspace Workspace { get; private set; }

        public event Action<SubLevelPhase> PhaseChanged;
        public event Action SubLevelFinished;

        // ChatBox pipeline — managed by base class
        private ChatBoxUI chatBox;
        private Coroutine pendingSubmitRoutine;

        public virtual void Initialize(DualWorldWorkspace workspace)
        {
            Workspace = workspace;
            OnInitialized();
        }

        public void EnterPhase(SubLevelPhase phase)
        {
            CurrentPhase = phase;
            OnPhaseEntered(phase);
            PhaseChanged?.Invoke(phase);

            if (phase == SubLevelPhase.SubLevelCompleted)
            {
                SubLevelFinished?.Invoke();
            }
        }

        public abstract void OnRealitySubmit(RealitySubmitResult result);
        public abstract void OnDreamComplete();
        public abstract void OnTraversalReachedExit();

        protected virtual void OnInitialized() { }
        protected virtual void OnPhaseEntered(SubLevelPhase phase) { }

        // ─────────────────────────────────────
        // ChatBox pipeline (shared by all sub-levels)
        // ─────────────────────────────────────

        /// <summary>子类在 OnInitialized 末尾调用，订阅 ChatBox submit 按钮</summary>
        protected void SubscribeToChatBox()
        {
            chatBox = Workspace?.ChatTaskController?.ChatBox
                      ?? FindObjectOfType<ChatBoxUI>(true);
            if (chatBox != null) chatBox.SubmitRequested += HandleSubmitRequested;
        }

        /// <summary>子类在 OnDestroy 中调用</summary>
        protected void UnsubscribeFromChatBox()
        {
            if (chatBox != null) chatBox.SubmitRequested -= HandleSubmitRequested;
        }

        protected void PublishChatTask()
        {
            if (taskDefinition != null)
                Workspace?.ChatTaskController?.Publish(taskDefinition);
        }

        protected void RaiseChatEvent(ChatTaskController.Event evt)
        {
            Workspace?.ChatTaskController?.Raise(evt);
        }

        protected void AppendPlayerSubmitLine()
        {
            Workspace?.ChatTaskController?.AppendPlayerSubmit();
        }

        protected void SetSubmitInteractable(bool v)
        {
            if (chatBox != null) chatBox.SetSubmitInteractable(v);
        }

        protected void GoSuccessScene()
        {
            if (!string.IsNullOrWhiteSpace(successSceneName))
                StartCoroutine(DelayThenGoScene());
        }

        protected IEnumerator DelayThen(float seconds, Action action)
        {
            if (seconds > 0f) yield return new WaitForSeconds(seconds);
            action?.Invoke();
        }

        /// <summary>当前是否可以提交 reality 任务</summary>
        protected abstract bool CanSubmit();

        /// <summary>执行实际的 reality 任务提交</summary>
        protected abstract void DoSubmitRealityTask();

        // ── private impl ──

        private void HandleSubmitRequested()
        {
            if (!CanSubmit()) return;

            AppendPlayerSubmitLine();

            if (submitToNpcReplyDelaySec > 0f && isActiveAndEnabled)
            {
                if (pendingSubmitRoutine != null) StopCoroutine(pendingSubmitRoutine);
                pendingSubmitRoutine = StartCoroutine(
                    DelayThen(submitToNpcReplyDelaySec, SubmitRealityTask));
            }
            else SubmitRealityTask();
        }

        private void SubmitRealityTask()
        {
            pendingSubmitRoutine = null;
            if (!CanSubmit()) return;
            DoSubmitRealityTask();
        }

        private IEnumerator DelayThenGoScene()
        {
            if (successTransitionDelaySec > 0f)
                yield return new WaitForSeconds(successTransitionDelaySec);
            SceneRouter.GoScene(successSceneName);
        }
    }
}
