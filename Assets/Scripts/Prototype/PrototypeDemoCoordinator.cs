using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace GameCreate3
{
    public sealed class PrototypeDemoCoordinator : MonoBehaviour
    {
        public readonly struct ColorSubmitResult
        {
            public ColorSubmitResult(bool success)
            {
                this.success = success;
            }

            public bool success { get; }
        }

        public sealed class ColorPuzzleController : MonoBehaviour
        {
            public event System.Action<ColorSubmitResult> OnSubmitAttempted;

            public void ResetPuzzle()
            {
                _ = OnSubmitAttempted;
            }

            public void SetDreamPaletteEnabled(bool enabled)
            {
            }

            public void SetDreamPaletteColors(IReadOnlyList<PaletteColorOption> options)
            {
            }

            public void SetInteractable(bool interactable)
            {
            }
        }

        public sealed class DreamColorCollectController : MonoBehaviour
        {
            public event System.Action<IReadOnlyList<PaletteColorOption>> Completed;

            public void ResetStage()
            {
                _ = Completed;
            }

            public void SetInteractive(bool interactive)
            {
            }
        }

        [Header("Camera")]
        [SerializeField] private Camera worldCamera;
        [SerializeField] private SideScrollCameraFollow cameraFollow;

        [Header("Screen Frames")]
        [SerializeField] private RectTransform leftFrame;
        [SerializeField] private RectTransform rightFrame;
        [SerializeField] private Image leftMask;
        [SerializeField] private Image rightMask;
        [SerializeField] private Text stageLabel;
        [SerializeField] private Text leftHint;
        [SerializeField] private Text rightHint;
        [SerializeField] private Text completionLabel;

        [Header("Player")]
        [SerializeField] private Transform player;
        [SerializeField] private SideScrollerPlayerController playerController;
        [SerializeField] private Transform stage1Spawn;
        [SerializeField] private Transform stage2Spawn;

        [Header("Stage Roots")]
        [SerializeField] private GameObject stage1LeftRoot;
        [SerializeField] private GameObject stage2LeftRoot;
        [SerializeField] private GameObject stage1RightRoot;
        [SerializeField] private GameObject stage2RightRoot;

        [Header("Stage Controllers")]
        [SerializeField] private LayoutPuzzleController stage1RightController;
        [SerializeField] private DreamGoalTrigger stage1DreamController;
        [SerializeField] private LeftWorldActivationController stage1LeftController;
        [SerializeField] private PrototypeDemoCoordinator.ColorPuzzleController stage2RightController;
        [SerializeField] private PrototypeDemoCoordinator.DreamColorCollectController stage2LeftController;

        private ChapterState currentState;
        private bool subscriptionsBound;

        private void OnEnable()
        {
            BindSubscriptions();
        }

        private void OnDisable()
        {
            UnbindSubscriptions();
        }

        private void Start()
        {
            BeginChapter();
        }

        public void InitializeRuntime(
            Camera runtimeWorldCamera,
            SideScrollCameraFollow runtimeCameraFollow,
            RectTransform runtimeLeftFrame,
            RectTransform runtimeRightFrame,
            Image runtimeLeftMask,
            Image runtimeRightMask,
            Text runtimeStageLabel,
            Text runtimeLeftHint,
            Text runtimeRightHint,
            Text runtimeCompletionLabel,
            Transform runtimePlayer,
            SideScrollerPlayerController runtimePlayerController,
            Transform runtimeStage1Spawn,
            Transform runtimeStage2Spawn,
            GameObject runtimeStage1LeftRoot,
            GameObject runtimeStage2LeftRoot,
            GameObject runtimeStage1RightRoot,
            GameObject runtimeStage2RightRoot,
            LayoutPuzzleController runtimeStage1RightController,
            DreamGoalTrigger runtimeStage1DreamController,
            LeftWorldActivationController runtimeStage1LeftController,
            PrototypeDemoCoordinator.ColorPuzzleController runtimeStage2RightController,
            PrototypeDemoCoordinator.DreamColorCollectController runtimeStage2LeftController)
        {
            worldCamera = runtimeWorldCamera;
            cameraFollow = runtimeCameraFollow;
            leftFrame = runtimeLeftFrame;
            rightFrame = runtimeRightFrame;
            leftMask = runtimeLeftMask;
            rightMask = runtimeRightMask;
            stageLabel = runtimeStageLabel;
            leftHint = runtimeLeftHint;
            rightHint = runtimeRightHint;
            completionLabel = runtimeCompletionLabel;
            player = runtimePlayer;
            playerController = runtimePlayerController;
            stage1Spawn = runtimeStage1Spawn;
            stage2Spawn = runtimeStage2Spawn;
            stage1LeftRoot = runtimeStage1LeftRoot;
            stage2LeftRoot = runtimeStage2LeftRoot;
            stage1RightRoot = runtimeStage1RightRoot;
            stage2RightRoot = runtimeStage2RightRoot;
            stage1RightController = runtimeStage1RightController;
            stage1DreamController = runtimeStage1DreamController;
            stage1LeftController = runtimeStage1LeftController;
            stage2RightController = runtimeStage2RightController;
            stage2LeftController = runtimeStage2LeftController;

            if (isActiveAndEnabled)
            {
                BindSubscriptions();
            }
        }

        public void BeginChapter()
        {
            if (completionLabel != null)
            {
                completionLabel.gameObject.SetActive(false);
            }

            stage1RightController?.ResetPuzzle();
            stage1DreamController?.ResetGoal();
            stage2RightController?.ResetPuzzle();
            stage2LeftController?.ResetStage();
            SetStage1Active(true);
            SetStage2Active(false);
            MovePlayerTo(stage1Spawn);
            ApplyLayout(splitView: false);
            ApplyStageLabel("第二章 阶段一：右屏排版");
            ApplyLeftState(false, "梦境暂未开启");
            ApplyRightState(true, "拖动模块并点击提交");
            currentState = ChapterState.Stage1RightFullScreen;
        }

        private void HandleStage1Submit(LayoutSubmitResult result)
        {
            switch (currentState)
            {
                case ChapterState.Stage1RightFullScreen:
                    ApplyLayout(splitView: true);
                    ApplyStageLabel("阶段一：进入梦境，恢复空间感");
                    ApplyLeftState(true, "把梦境块推到目标位");
                    ApplyRightState(false, "右屏暂时冻结，左侧继续");
                    currentState = ChapterState.Stage1LeftUnlocked;
                    break;

                case ChapterState.Stage1RightAssisted:
                    if (!result.success)
                    {
                        ApplyStageLabel("阶段一：梦境已经帮助你校准排版，再试一次");
                        return;
                    }

                    currentState = ChapterState.Stage1Complete;
                    EnterStage2();
                    break;
            }
        }

        private void HandleStage1DreamComplete()
        {
            if (currentState != ChapterState.Stage1LeftUnlocked)
            {
                return;
            }

            stage1LeftController?.MarkCompleted();
            stage1RightController?.SetAssistEnabled(true);
            ApplyStageLabel("阶段一：回到右屏完成排版");
            ApplyLeftState(true, "梦境仍可自由移动");
            ApplyRightState(true, "目标位已高亮，模块会自动吸附");
            currentState = ChapterState.Stage1RightAssisted;
        }

        private void EnterStage2()
        {
            SetStage1Active(false);
            SetStage2Active(true);
            MovePlayerTo(stage2Spawn);
            ApplyLayout(splitView: false);
            ApplyStageLabel("第二章 阶段二：右屏换图");
            ApplyLeftState(false, "等待右屏再次卡住后解锁");
            ApplyRightState(true, "先试着替换组件并提交");
            stage2RightController?.SetDreamPaletteEnabled(false);
            currentState = ChapterState.Stage2RightFullScreen;
        }

        private void HandleStage2Submit(PrototypeDemoCoordinator.ColorSubmitResult result)
        {
            switch (currentState)
            {
                case ChapterState.Stage2RightFullScreen:
                    ApplyLayout(splitView: true);
                    ApplyStageLabel("阶段二：进入梦境，收集正确图卡");
                    if (stage2LeftController != null)
                    {
                        stage2LeftController.SetInteractive(true);
                    }
                    ApplyLeftState(true, "收集正确图卡");
                    ApplyRightState(false, "右屏暂时冻结，先去梦境寻找正确样式");
                    currentState = ChapterState.Stage2LeftUnlocked;
                    break;

                case ChapterState.Stage2RightAssisted:
                    if (!result.success)
                    {
                        ApplyStageLabel("阶段二：使用梦境图卡完成替换");
                        return;
                    }

                    currentState = ChapterState.Stage2Complete;
                    FinishChapter();
                    break;
            }
        }

        private void HandleStage2DreamComplete(IReadOnlyList<PaletteColorOption> colors)
        {
            if (currentState != ChapterState.Stage2LeftUnlocked)
            {
                return;
            }

            stage2RightController?.SetDreamPaletteColors(colors);
            stage2RightController?.SetDreamPaletteEnabled(true);
            ApplyStageLabel("阶段二：回到右屏使用梦境图卡");
            ApplyLeftState(true, "梦境仍可自由移动");
            ApplyRightState(true, "梦境图卡已解锁，选择图卡并替换目标组件");
            currentState = ChapterState.Stage2RightAssisted;
        }

        private void FinishChapter()
        {
            ApplyLayout(splitView: true);
            ApplyStageLabel("第二章完成");
            ApplyLeftState(false, "本章完成");
            ApplyRightState(false, "本章完成");
            if (completionLabel != null)
            {
                completionLabel.text = "第二章大原型已完成";
                completionLabel.gameObject.SetActive(true);
            }

            currentState = ChapterState.ChapterComplete;
        }

        private void BindSubscriptions()
        {
            UnbindSubscriptions();

            if (stage1RightController != null)
            {
                stage1RightController.OnSubmitAttempted += HandleStage1Submit;
            }

            if (stage1DreamController != null)
            {
                stage1DreamController.Completed += HandleStage1DreamComplete;
            }

            if (stage2RightController != null)
            {
                stage2RightController.OnSubmitAttempted += HandleStage2Submit;
            }

            if (stage2LeftController != null)
            {
                stage2LeftController.Completed += HandleStage2DreamComplete;
            }

            subscriptionsBound = true;
        }

        private void UnbindSubscriptions()
        {
            if (!subscriptionsBound)
            {
                return;
            }

            if (stage1RightController != null)
            {
                stage1RightController.OnSubmitAttempted -= HandleStage1Submit;
            }

            if (stage1DreamController != null)
            {
                stage1DreamController.Completed -= HandleStage1DreamComplete;
            }

            if (stage2RightController != null)
            {
                stage2RightController.OnSubmitAttempted -= HandleStage2Submit;
            }

            if (stage2LeftController != null)
            {
                stage2LeftController.Completed -= HandleStage2DreamComplete;
            }

            subscriptionsBound = false;
        }

        private void SetStage1Active(bool active)
        {
            if (stage1LeftRoot != null)
            {
                stage1LeftRoot.SetActive(active);
            }

            if (stage1RightRoot != null)
            {
                stage1RightRoot.SetActive(active);
            }
        }

        private void SetStage2Active(bool active)
        {
            if (stage2LeftRoot != null)
            {
                stage2LeftRoot.SetActive(active);
            }

            if (stage2RightRoot != null)
            {
                stage2RightRoot.SetActive(active);
            }
        }

        private void MovePlayerTo(Transform spawn)
        {
            if (player == null || spawn == null)
            {
                return;
            }

            player.position = spawn.position;

            var body = player.GetComponent<Rigidbody2D>();
            if (body != null)
            {
                body.velocity = Vector2.zero;
                body.angularVelocity = 0f;
            }
        }

        private void ApplyLeftState(bool interactable, string hint)
        {
            if (playerController != null)
            {
                playerController.SetInputLocked(!interactable);
            }

            if (stage1LeftController != null && stage1LeftRoot != null && stage1LeftRoot.activeSelf)
            {
                stage1LeftController.SetInteractive(interactable);
            }

            if (stage2LeftController != null && stage2LeftRoot != null && stage2LeftRoot.activeSelf)
            {
                stage2LeftController.SetInteractive(interactable);
            }

            if (leftMask != null)
            {
                leftMask.color = interactable
                    ? new Color(0.04f, 0.05f, 0.07f, 0.14f)
                    : new Color(0.05f, 0.06f, 0.08f, 0.68f);
            }

            if (leftHint != null)
            {
                leftHint.text = hint;
                leftHint.gameObject.SetActive(!interactable);
            }
        }

        private void ApplyRightState(bool interactable, string hint)
        {
            if (stage1RightRoot != null && stage1RightRoot.activeSelf)
            {
                stage1RightController?.SetInteractable(interactable);
            }

            if (stage2RightRoot != null && stage2RightRoot.activeSelf)
            {
                stage2RightController?.SetInteractable(interactable);
            }

            if (rightMask != null)
            {
                rightMask.color = interactable
                    ? new Color(0.03f, 0.04f, 0.05f, 0.08f)
                    : new Color(0.03f, 0.04f, 0.05f, 0.6f);
            }

            if (rightHint != null)
            {
                rightHint.text = hint;
                rightHint.gameObject.SetActive(!interactable);
            }
        }

        private void ApplyStageLabel(string text)
        {
            if (stageLabel != null)
            {
                stageLabel.text = text;
            }
        }

        private void ApplyLayout(bool splitView)
        {
            if (worldCamera != null)
            {
                worldCamera.rect = splitView ? new Rect(0f, 0f, 0.5f, 1f) : new Rect(0f, 0f, 0f, 0f);
            }

            if (leftFrame != null)
            {
                leftFrame.gameObject.SetActive(splitView);
            }

            if (rightFrame != null)
            {
                if (splitView)
                {
                    Stretch(rightFrame, new Vector2(0.5f, 0f), new Vector2(1f, 1f), new Vector2(20f, 20f), new Vector2(-20f, -20f));
                }
                else
                {
                    Stretch(rightFrame, new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(20f, 20f), new Vector2(-20f, -20f));
                }
            }
        }

        private static void Stretch(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
        {
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;
            rect.localScale = Vector3.one;
        }

        private enum ChapterState
        {
            Stage1RightFullScreen,
            Stage1LeftUnlocked,
            Stage1RightAssisted,
            Stage1Complete,
            Stage2RightFullScreen,
            Stage2LeftUnlocked,
            Stage2RightAssisted,
            Stage2Complete,
            ChapterComplete
        }
    }
}
