using UnityEngine;

namespace GameCreate3.StoryPlayer
{
    /// <summary>
    /// 生产用 Rig 的轻量初始化器。挂在 StoryPlayerRig.prefab 根节点上。
    /// Awake 里把同一 prefab 里预接好的 PageRenderer / TransitionController / InputController
    /// 串到 StoryPlayer，让 <see cref="StoryPlayerService.Play"/> 可以直接调用 player.Play()。
    /// 与已废弃的 <see cref="StoryPlayerTestBootstrap"/> 区别：不自搭 UI、不自行 Play、不生成测试数据。
    /// </summary>
    [DefaultExecutionOrder(-100)]
    public sealed class StoryRigBootstrap : MonoBehaviour
    {
        [SerializeField] private StoryPlayer storyPlayer;
        [SerializeField] private StoryPageRenderer pageRenderer;
        [SerializeField] private SimpleTransitionController transitionController;
        [SerializeField] private StoryInputController inputController;
        [SerializeField] private StoryFlowBridge flowBridge;
        [Tooltip("剧情播放期间需要显示／结束后需要隐藏的 UI 根（通常是 StoryCanvas）。")]
        [SerializeField] private GameObject canvasRoot;

        private void Awake()
        {
            if (storyPlayer == null || pageRenderer == null || transitionController == null)
            {
                Debug.LogError("[StoryRigBootstrap] Missing required references on rig prefab.");
                return;
            }

            storyPlayer.Initialize(pageRenderer, transitionController);
            storyPlayer.OnStateChanged += HandleStateChanged;

            if (inputController != null)
            {
                inputController.Initialize(storyPlayer, pageRenderer);
                inputController.OnNextPageRequested += storyPlayer.NextPage;
                inputController.OnSkipSequenceRequested += storyPlayer.SkipSequence;
                inputController.OnTextFastForwardRequested += pageRenderer.SkipCurrentAnimation;
            }

            if (flowBridge != null)
            {
                flowBridge.BindStoryPlayer(storyPlayer);
            }

            // 默认隐藏，等 Play 触发 PlayingPage 状态再亮起
            SetCanvasVisible(false);
        }

        private void OnDestroy()
        {
            if (storyPlayer != null)
            {
                storyPlayer.OnStateChanged -= HandleStateChanged;
            }
        }

        private void HandleStateChanged(StoryPlayerState state)
        {
            switch (state)
            {
                case StoryPlayerState.PlayingPage:
                case StoryPlayerState.WaitingInput:
                case StoryPlayerState.Transitioning:
                    SetCanvasVisible(true);
                    break;
                case StoryPlayerState.Idle:
                case StoryPlayerState.Completed:
                case StoryPlayerState.Skipped:
                    SetCanvasVisible(false);
                    break;
            }
        }

        private void SetCanvasVisible(bool visible)
        {
            if (canvasRoot != null && canvasRoot.activeSelf != visible)
            {
                canvasRoot.SetActive(visible);
            }
        }
    }
}
