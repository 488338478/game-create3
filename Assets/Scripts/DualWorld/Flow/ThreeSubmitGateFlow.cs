using System.Collections;
using GameCreate3.Core;
using GameCreate3.Core.SceneRouting;
using UnityEngine;

namespace GameCreate3.DualWorld
{
    /// <summary>
    /// 简易三击闸门：submit 按钮被点击 N 次后白场过渡到目标场景。
    /// 用于 L1Cutscene 和 L2 之间的过渡关卡。
    /// </summary>
    public sealed class ThreeSubmitGateFlow : MonoBehaviour
    {
        [Header("Whiteout")]
        [SerializeField] private ScreenWhiteout whiteout;
        [SerializeField] private float whiteoutDuration = 0.8f;
        [SerializeField] private float holdWhiteDelay = 0.4f;
        [Tooltip("最后一次 submit 后，先等这么久让聊天内容（玩家气泡 + NPC 回复）显示出来，再开始白场淡出。")]
        [SerializeField] private float submitToWhiteoutDelay = 1.5f;

        [Header("Gate")]
        [SerializeField] private int requiredSubmits = 3;
        [SerializeField] private string targetScene = "Level2";

        [Header("Reality Task")]
        [SerializeField] private RealityAlignmentTask alignmentTask;

        [Header("Chat")]
        [SerializeField] private ChatTaskDefinition taskDefinition;

        private ChatBoxUI chatBox;
        private ChatTaskController chatTaskController;
        private int submitCount;
        private bool transitioning;

        private IEnumerator Start()
        {
            chatBox = FindObjectOfType<ChatBoxUI>(true);
            chatTaskController = FindObjectOfType<ChatTaskController>(true);

            // 跨 prefab 无法序列化引用，运行时补查
            if (alignmentTask == null)
                alignmentTask = FindObjectOfType<RealityAlignmentTask>(true);

            if (chatBox != null)
                chatBox.SubmitRequested += HandleSubmit;

            if (chatTaskController != null && taskDefinition != null)
                chatTaskController.Publish(taskDefinition);

            if (chatBox != null)
                chatBox.SetSubmitInteractable(true);

            // 开场白屏 → 淡入
            if (whiteout != null)
            {
                whiteout.SetAlphaImmediate(1f);
                yield return null;
                whiteout.Reverse();
                yield return new WaitForSeconds(whiteoutDuration);
            }
        }

        private void OnDestroy()
        {
            if (chatBox != null)
                chatBox.SubmitRequested -= HandleSubmit;
        }

        private void HandleSubmit()
        {
            if (transitioning) return;

            // 只做计数闸门。AppendPlayerSubmit / alignmentTask.Submit 由
            // workspace 内部的 LevelInGameFlowController → SubLevelFlow 负责，
            // 不在此重复调用，否则每条 submit 会产生两条内容。

            submitCount++;

            if (submitCount >= requiredSubmits)
            {
                transitioning = true;
                // 只锁提交按钮防止再点。注意：不要在这里禁用 reality 任务的交互，
                // 否则 workspace 的 SubLevelFlow.CanSubmit()（=realityTask.IsInteractable）
                // 会跳过这一次 submit，导致第三条聊天内容（玩家气泡 + NPC 回复）不出现。
                // reality 任务由 workspace 自己在 RealityTaskCompleted 阶段关闭。
                if (chatBox != null) chatBox.SetSubmitInteractable(false);
                StartCoroutine(TransitionOut());
            }
        }

        private IEnumerator TransitionOut()
        {
            // 先留出时间让最后一条 submit 的聊天内容（玩家气泡 + NPC 回复）显示出来，再开始白场。
            if (submitToWhiteoutDelay > 0f)
                yield return new WaitForSeconds(submitToWhiteoutDelay);

            if (whiteout != null)
            {
                whiteout.Trigger();
                yield return new WaitForSeconds(whiteoutDuration + holdWhiteDelay);
            }

            // BGM 由 GameAudioService（DontDestroyOnLoad 单例）持有，
            // 标记跳过下一次 FadeOut 即可顺延到下一场景。
            GameAudioService.SkipNextBgmFadeOut = true;

            // 把 reality 散块位移 + 聊天 log 打包带到下一关，由 L2 的 DualWorldHandoffRestorer 还原。
            var handoff = new DualWorldHandoff.Snapshot();
            if (alignmentTask != null) alignmentTask.CaptureBlocks(handoff);
            if (chatBox != null) handoff.chat = chatBox.GetLog();
            DualWorldHandoff.Pending = handoff;

            SceneRouter.GoScene(targetScene);
        }
    }
}
