using System.Collections;
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

            chatTaskController?.AppendPlayerSubmit();
            alignmentTask?.Submit();

            submitCount++;

            if (submitCount >= requiredSubmits)
            {
                transitioning = true;
                if (chatBox != null) chatBox.SetSubmitInteractable(false);
                alignmentTask?.SetInteractable(false);
                StartCoroutine(TransitionOut());
            }
        }

        private IEnumerator TransitionOut()
        {
            if (whiteout != null)
            {
                whiteout.Trigger();
                yield return new WaitForSeconds(whiteoutDuration + holdWhiteDelay);
            }

            SceneRouter.GoScene(targetScene);
        }
    }
}
