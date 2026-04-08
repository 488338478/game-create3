using UnityEngine;
using UnityEngine.UI;

namespace GameCreate3
{
    public sealed class PrototypeDemoCoordinator : MonoBehaviour
    {
        private DemoState currentState;
        private Camera worldCamera;
        private LayoutPuzzleController layoutPuzzle;
        private LeftWorldActivationController leftWorld;
        private DreamGoalTrigger dreamGoal;
        private RectTransform leftFrame;
        private RectTransform rightFrame;
        private Text stageLabel;
        private Text leftStateText;
        private Text rightStateText;
        private Image leftMask;
        private Image rightMask;
        private Text completionLabel;

        public void Initialize(
            Camera gameplayCamera,
            LayoutPuzzleController puzzle,
            LeftWorldActivationController leftWorldController,
            DreamGoalTrigger goalTrigger,
            RectTransform leftWorldFrame,
            RectTransform rightWorldFrame,
            Text stageText,
            Text leftHintText,
            Text rightHintText,
            Image leftOverlayMask,
            Image rightOverlayMask,
            Text completionText)
        {
            worldCamera = gameplayCamera;
            layoutPuzzle = puzzle;
            leftWorld = leftWorldController;
            dreamGoal = goalTrigger;
            leftFrame = leftWorldFrame;
            rightFrame = rightWorldFrame;
            stageLabel = stageText;
            leftStateText = leftHintText;
            rightStateText = rightHintText;
            leftMask = leftOverlayMask;
            rightMask = rightOverlayMask;
            completionLabel = completionText;

            if (dreamGoal != null)
            {
                dreamGoal.Completed += HandleDreamComplete;
            }
        }

        private void OnDestroy()
        {
            if (dreamGoal != null)
            {
                dreamGoal.Completed -= HandleDreamComplete;
            }
        }

        public void BeginDemo()
        {
            currentState = DemoState.Intro;
            ApplyLayout(splitView: false);
            ApplyRightWorldState(interactable: true, stateText: "拖动模块并点击提交");
            ApplyLeftWorldState(interactable: false, stateText: "现实卡住后，梦境才会接管");
            SetStage("第二章 DEMO：右屏排版卡关");
            completionLabel.gameObject.SetActive(false);
            currentState = DemoState.RightPuzzleBeforeDream;
        }

        public void HandleRightSubmit(LayoutSubmitResult result)
        {
            if (layoutPuzzle == null)
            {
                return;
            }

            switch (currentState)
            {
                case DemoState.RightPuzzleBeforeDream:
                    currentState = DemoState.BossReject;
                    ApplyLayout(splitView: true);
                    SetStage("老板否掉了方案，梦境开始浮现");
                    ApplyRightWorldState(interactable: false, stateText: "右屏暂时冻结，去左侧寻找答案");
                    ApplyLeftWorldState(interactable: true, stateText: "控制角色跳到终点，修复空间感");
                    currentState = DemoState.LeftPlatformSection;
                    break;

                case DemoState.RightPuzzleAfterDream:
                    if (result.success)
                    {
                        currentState = DemoState.BossApprove;
                        SetStage("老板通过了排版方案");
                        ApplyRightWorldState(interactable: false, stateText: "排版通过");
                        ApplyLeftWorldState(interactable: false, stateText: "梦境已稳定");
                        completionLabel.text = "Demo 完成：双世界联动闭环已验证";
                        completionLabel.gameObject.SetActive(true);
                        currentState = DemoState.DemoComplete;
                    }
                    else
                    {
                        SetStage("梦境已经帮你校准感觉，再试一次排版");
                    }

                    break;
            }
        }

        public void HandleDreamComplete()
        {
            if (currentState != DemoState.LeftPlatformSection || layoutPuzzle == null)
            {
                return;
            }

            leftWorld.MarkCompleted();
            layoutPuzzle.SetAssistEnabled(true);
            ApplyLeftWorldState(interactable: true, stateText: "梦境仍可自由移动");
            ApplyRightWorldState(interactable: true, stateText: "现在模块会吸附，重新提交");
            SetStage("回到右屏：梦境让排版变得顺手");
            currentState = DemoState.RightPuzzleAfterDream;
        }

        private void ApplyLeftWorldState(bool interactable, string stateText)
        {
            if (leftWorld != null)
            {
                leftWorld.SetInteractive(interactable);
            }

            if (leftMask != null)
            {
                leftMask.color = interactable
                    ? new Color(0.04f, 0.05f, 0.07f, 0.14f)
                    : new Color(0.05f, 0.06f, 0.08f, 0.68f);
            }

            if (leftStateText != null)
            {
                leftStateText.text = stateText;
                leftStateText.gameObject.SetActive(!interactable);
            }
        }

        private void ApplyRightWorldState(bool interactable, string stateText)
        {
            if (layoutPuzzle != null)
            {
                layoutPuzzle.SetInteractable(interactable);
            }

            if (rightMask != null)
            {
                rightMask.color = interactable
                    ? new Color(0.03f, 0.04f, 0.05f, 0.08f)
                    : new Color(0.03f, 0.04f, 0.05f, 0.6f);
            }

            if (rightStateText != null)
            {
                rightStateText.text = stateText;
                rightStateText.gameObject.SetActive(!interactable);
            }
        }

        private void SetStage(string text)
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

        private enum DemoState
        {
            Intro,
            RightPuzzleBeforeDream,
            BossReject,
            LeftPlatformSection,
            RightPuzzleAfterDream,
            BossApprove,
            DemoComplete
        }
    }
}
