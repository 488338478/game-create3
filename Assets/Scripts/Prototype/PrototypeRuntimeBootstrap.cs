using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace GameCreate3
{
    public sealed class PrototypeRuntimeBootstrap : MonoBehaviour
    {
        private const string BootstrapObjectName = "_PrototypeRuntimeBootstrap";

        private static Sprite whiteSprite;
        private static Font runtimeFont;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void EnsureBootstrap()
        {
            var activeScene = SceneManager.GetActiveScene();
            if (!activeScene.IsValid() || activeScene.name != "SampleScene")
            {
                return;
            }

            if (FindObjectOfType<PrototypeRuntimeBootstrap>() != null)
            {
                return;
            }

            var bootstrap = new GameObject(BootstrapObjectName);
            bootstrap.AddComponent<PrototypeRuntimeBootstrap>();
        }

        private void Start()
        {
            BuildPrototype();
        }

        private void BuildPrototype()
        {
            if (GameObject.Find("PrototypeDemo") != null)
            {
                return;
            }

            EnsureEventSystem();

            var camera = SetupCamera();
            var sceneRoot = new GameObject("PrototypeDemo");
            var coordinator = sceneRoot.AddComponent<PrototypeDemoCoordinator>();

            var leftWorld = BuildLeftWorld(sceneRoot.transform, camera);
            var uiReferences = BuildScreenUi(sceneRoot.transform);
            var layoutPuzzle = BuildRightPuzzle(uiReferences, coordinator);

            coordinator.Initialize(
                camera,
                layoutPuzzle,
                leftWorld.ActivationController,
                leftWorld.GoalTrigger,
                uiReferences.LeftFrame,
                uiReferences.RightFrame,
                uiReferences.StageLabel,
                uiReferences.LeftStateText,
                uiReferences.RightStateText,
                uiReferences.LeftMask,
                uiReferences.RightMask,
                uiReferences.CompletionLabel);

            coordinator.BeginDemo();
        }

        private static void EnsureEventSystem()
        {
            if (FindObjectOfType<EventSystem>() != null)
            {
                return;
            }

            var eventSystem = new GameObject("EventSystem");
            eventSystem.AddComponent<EventSystem>();
            eventSystem.AddComponent<StandaloneInputModule>();
        }

        private static Camera SetupCamera()
        {
            var camera = Camera.main ?? FindObjectOfType<Camera>();
            if (camera == null)
            {
                camera = new GameObject("Main Camera").AddComponent<Camera>();
                camera.tag = "MainCamera";
            }

            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.09f, 0.1f, 0.15f);
            camera.orthographic = true;
            camera.orthographicSize = 5.6f;
            camera.rect = new Rect(0f, 0f, 0.5f, 1f);
            camera.transform.position = new Vector3(0f, 1.6f, -10f);

            return camera;
        }

        private LeftWorldReferences BuildLeftWorld(Transform parent, Camera camera)
        {
            var root = new GameObject("LeftWorld");
            root.transform.SetParent(parent, false);

            CreateWorldBackdrop(root.transform);
            CreateGround(root.transform, new Vector2(0f, -2.6f), new Vector2(15f, 1f), new Color(0.34f, 0.4f, 0.48f));
            CreateGround(root.transform, new Vector2(2.2f, -1.2f), new Vector2(2.4f, 0.35f), new Color(0.48f, 0.58f, 0.72f));
            CreateGround(root.transform, new Vector2(5.4f, 0.35f), new Vector2(2.4f, 0.35f), new Color(0.58f, 0.7f, 0.81f));
            CreateGround(root.transform, new Vector2(8.3f, 1.55f), new Vector2(2.2f, 0.35f), new Color(0.67f, 0.79f, 0.88f));

            var player = CreatePlayer(root.transform);
            var goal = CreateGoal(root.transform);
            var destination = CreateGoalDestination(root.transform, goal.transform.position.y);
            goal.Initialize(goal.transform, destination.transform);

            var cameraFollow = camera.gameObject.GetComponent<SideScrollCameraFollow>();
            if (cameraFollow == null)
            {
                cameraFollow = camera.gameObject.AddComponent<SideScrollCameraFollow>();
            }

            SetPrivateField(cameraFollow, "target", player.transform);
            SetPrivateField(cameraFollow, "offset", new Vector3(0f, 1f, -10f));
            SetPrivateField(cameraFollow, "smoothTime", 0.18f);
            SetPrivateField(cameraFollow, "useBounds", true);
            SetPrivateField(cameraFollow, "minX", -1f);
            SetPrivateField(cameraFollow, "maxX", 8f);
            SetPrivateField(cameraFollow, "minY", -1f);
            SetPrivateField(cameraFollow, "maxY", 3.5f);

            var activationController = root.AddComponent<LeftWorldActivationController>();
            activationController.Initialize(player.GetComponent<SideScrollerPlayerController>(), goal.GetComponent<SpriteRenderer>());

            return new LeftWorldReferences(activationController, goal);
        }

        private static void CreateWorldBackdrop(Transform parent)
        {
            var bg = CreateSpriteObject("Backdrop", parent, new Vector2(3.8f, 0.9f), new Vector2(13.8f, 8.4f), new Color(0.16f, 0.17f, 0.24f));
            bg.sortingOrder = -20;

            var panelA = CreateSpriteObject("PanelA", parent, new Vector2(-0.3f, -0.2f), new Vector2(3.5f, 3.5f), new Color(0.29f, 0.31f, 0.41f));
            panelA.sortingOrder = -18;

            var panelB = CreateSpriteObject("PanelB", parent, new Vector2(4.2f, 1.4f), new Vector2(4.3f, 4.2f), new Color(0.22f, 0.24f, 0.34f));
            panelB.sortingOrder = -19;
        }

        private static void CreateGround(Transform parent, Vector2 position, Vector2 size, Color color)
        {
            var ground = new GameObject("Ground");
            ground.transform.SetParent(parent, false);
            ground.transform.position = position;

            var renderer = ground.AddComponent<SpriteRenderer>();
            renderer.sprite = GetWhiteSprite();
            renderer.color = color;
            renderer.drawMode = SpriteDrawMode.Sliced;
            renderer.size = size;

            var collider = ground.AddComponent<BoxCollider2D>();
            collider.size = size;
        }

        private static GameObject CreatePlayer(Transform parent)
        {
            var player = new GameObject("DreamPlayer");
            player.transform.SetParent(parent, false);
            player.transform.position = new Vector3(-4.6f, -1.65f, 0f);
            player.tag = "Player";

            var renderer = player.AddComponent<SpriteRenderer>();
            renderer.sprite = GetWhiteSprite();
            renderer.color = new Color(0.98f, 0.87f, 0.48f);
            renderer.drawMode = SpriteDrawMode.Sliced;
            renderer.size = new Vector2(0.7f, 1.1f);

            var bodyCollider = player.AddComponent<BoxCollider2D>();
            bodyCollider.size = new Vector2(0.7f, 1.1f);

            var rigidbody = player.AddComponent<Rigidbody2D>();
            rigidbody.gravityScale = 3.3f;
            rigidbody.freezeRotation = true;

            var groundCheck = new GameObject("GroundCheck");
            groundCheck.transform.SetParent(player.transform, false);
            groundCheck.transform.localPosition = new Vector3(0f, -0.58f, 0f);

            var controller = player.AddComponent<SideScrollerPlayerController>();
            SetPrivateField(controller, "groundCheckPoint", groundCheck.transform);
            SetPrivateField(controller, "groundCheckRadius", 0.22f);
            SetPrivateField(controller, "moveSpeed", 5.3f);
            SetPrivateField(controller, "jumpForce", 9.8f);

            return player;
        }

        private static DreamGoalTrigger CreateGoal(Transform parent)
        {
            var goalRoot = new GameObject("DreamGoal");
            goalRoot.transform.SetParent(parent, false);
            goalRoot.transform.position = new Vector3(8.5f, 2.3f, 0f);

            var renderer = goalRoot.AddComponent<SpriteRenderer>();
            renderer.sprite = GetWhiteSprite();
            renderer.color = new Color(0.55f, 0.91f, 0.77f);
            renderer.drawMode = SpriteDrawMode.Sliced;
            renderer.size = new Vector2(0.9f, 1.6f);

            var collider = goalRoot.AddComponent<BoxCollider2D>();
            collider.size = new Vector2(0.9f, 1.6f);

            var rigidbody = goalRoot.AddComponent<Rigidbody2D>();
            rigidbody.gravityScale = 0f;
            rigidbody.freezeRotation = true;
            rigidbody.constraints = RigidbodyConstraints2D.FreezePositionY | RigidbodyConstraints2D.FreezeRotation;
            rigidbody.interpolation = RigidbodyInterpolation2D.Interpolate;

            goalRoot.AddComponent<DreamGoalAxisLock>();
            return goalRoot.AddComponent<DreamGoalTrigger>();
        }

        private static SpriteRenderer CreateGoalDestination(Transform parent, float yPosition)
        {
            var destinationRoot = new GameObject("DreamDestination");
            destinationRoot.transform.SetParent(parent, false);
            destinationRoot.transform.position = new Vector3(10f, yPosition, 0f);

            var renderer = destinationRoot.AddComponent<SpriteRenderer>();
            renderer.sprite = GetWhiteSprite();
            renderer.color = new Color(0.98f, 0.95f, 0.6f, 0.34f);
            renderer.drawMode = SpriteDrawMode.Sliced;
            renderer.size = new Vector2(0.9f, 1.6f);
            renderer.sortingOrder = -1;
            return renderer;
        }

        private ScreenUiReferences BuildScreenUi(Transform parent)
        {
            var canvasRoot = new GameObject("PrototypeCanvas", typeof(RectTransform));
            canvasRoot.transform.SetParent(parent, false);

            var canvas = canvasRoot.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasRoot.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
            canvasRoot.AddComponent<GraphicRaycaster>();

            var rootRect = canvasRoot.GetComponent<RectTransform>();
            rootRect.anchorMin = Vector2.zero;
            rootRect.anchorMax = Vector2.one;
            rootRect.offsetMin = Vector2.zero;
            rootRect.offsetMax = Vector2.zero;

            var divider = CreateUiPanel("Divider", rootRect, new Color(0.12f, 0.13f, 0.19f, 1f));
            Stretch(divider.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 1f), new Vector2(-3f, 0f), new Vector2(3f, 0f));
            divider.raycastTarget = false;

            var leftFrame = CreateUiPanel("LeftFrame", rootRect, new Color(0f, 0f, 0f, 0f));
            Stretch(leftFrame.rectTransform, new Vector2(0f, 0f), new Vector2(0.5f, 1f), new Vector2(20f, 20f), new Vector2(-20f, -20f));
            leftFrame.raycastTarget = false;
            var leftBorder = CreateUiPanel("LeftBorder", leftFrame.rectTransform, new Color(1f, 1f, 1f, 0.08f));
            Stretch(leftBorder.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            leftBorder.raycastTarget = false;
            var leftMask = CreateUiPanel("LeftMask", leftFrame.rectTransform, new Color(0.05f, 0.06f, 0.08f, 0.68f));
            Stretch(leftMask.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            leftMask.raycastTarget = false;
            var leftStateText = CreateText("LeftState", leftFrame.rectTransform, "梦境暂未开启", 34, TextAnchor.MiddleCenter, new Color(0.94f, 0.95f, 0.98f));
            Stretch(leftStateText.rectTransform, new Vector2(0.1f, 0.4f), new Vector2(0.9f, 0.6f), Vector2.zero, Vector2.zero);
            leftStateText.raycastTarget = false;

            var rightFrame = CreateUiPanel("RightFrame", rootRect, new Color(0.12f, 0.12f, 0.17f, 0.92f));
            Stretch(rightFrame.rectTransform, new Vector2(0.5f, 0f), new Vector2(1f, 1f), new Vector2(20f, 20f), new Vector2(-20f, -20f));
            rightFrame.raycastTarget = false;
            var rightHeader = CreateUiPanel("RightHeader", rightFrame.rectTransform, new Color(0.17f, 0.18f, 0.27f, 1f));
            Stretch(rightHeader.rectTransform, new Vector2(0f, 0.9f), new Vector2(1f, 1f), new Vector2(0f, -4f), Vector2.zero);
            rightHeader.raycastTarget = false;
            var rightTitle = CreateText("RightTitle", rightHeader.rectTransform, "设计软件 DEMO / 排版卡关中", 32, TextAnchor.MiddleLeft, Color.white);
            Stretch(rightTitle.rectTransform, Vector2.zero, Vector2.one, new Vector2(24f, 0f), new Vector2(-24f, 0f));
            rightTitle.raycastTarget = false;
            var rightMask = CreateUiPanel("RightMask", rightFrame.rectTransform, new Color(0.03f, 0.04f, 0.05f, 0.6f));
            Stretch(rightMask.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            rightMask.raycastTarget = false;
            var rightStateText = CreateText("RightState", rightFrame.rectTransform, "拖动模块并提交", 34, TextAnchor.MiddleCenter, new Color(0.96f, 0.97f, 0.99f));
            Stretch(rightStateText.rectTransform, new Vector2(0.15f, 0.42f), new Vector2(0.85f, 0.58f), Vector2.zero, Vector2.zero);
            rightStateText.raycastTarget = false;

            var stagePlate = CreateUiPanel("StagePlate", rootRect, new Color(0.08f, 0.09f, 0.14f, 0.86f));
            Stretch(stagePlate.rectTransform, new Vector2(0.31f, 0.92f), new Vector2(0.69f, 0.985f), Vector2.zero, Vector2.zero);
            stagePlate.raycastTarget = false;
            var stageLabel = CreateText("StageLabel", stagePlate.rectTransform, string.Empty, 28, TextAnchor.MiddleCenter, new Color(0.97f, 0.98f, 1f));
            Stretch(stageLabel.rectTransform, Vector2.zero, Vector2.one, new Vector2(20f, 0f), new Vector2(-20f, 0f));
            stageLabel.raycastTarget = false;

            var completionLabel = CreateText("CompletionLabel", rootRect, string.Empty, 44, TextAnchor.MiddleCenter, new Color(1f, 0.98f, 0.88f));
            Stretch(completionLabel.rectTransform, new Vector2(0.27f, 0.06f), new Vector2(0.73f, 0.18f), Vector2.zero, Vector2.zero);
            completionLabel.raycastTarget = false;
            completionLabel.gameObject.SetActive(false);

            return new ScreenUiReferences(
                leftFrame.rectTransform,
                rightFrame.rectTransform,
                rightMask,
                rightStateText,
                leftMask,
                leftStateText,
                stageLabel,
                completionLabel);
        }

        private LayoutPuzzleController BuildRightPuzzle(ScreenUiReferences ui, PrototypeDemoCoordinator coordinator)
        {
            var root = new GameObject("RightPuzzle", typeof(RectTransform));
            root.transform.SetParent(ui.RightFrame, false);
            var rootRect = root.GetComponent<RectTransform>();
            Stretch(rootRect, new Vector2(0f, 0f), new Vector2(1f, 0.9f), new Vector2(0f, 0f), new Vector2(0f, -12f));

            var canvasGroup = root.AddComponent<CanvasGroup>();
            var workspaceFrame = CreateUiPanel("WorkspaceFrame", rootRect, new Color(0.2f, 0.22f, 0.31f, 1f));
            Stretch(workspaceFrame.rectTransform, new Vector2(0.05f, 0.14f), new Vector2(0.78f, 0.95f), Vector2.zero, Vector2.zero);
            var workspace = CreateUiPanel("Workspace", workspaceFrame.rectTransform, new Color(0.94f, 0.95f, 0.98f, 1f));
            Stretch(workspace.rectTransform, new Vector2(0.04f, 0.04f), new Vector2(0.96f, 0.96f), Vector2.zero, Vector2.zero);

            var feedbackRoot = CreateUiPanel("FeedbackRoot", rootRect, new Color(0.13f, 0.14f, 0.2f, 0.96f));
            Stretch(feedbackRoot.rectTransform, new Vector2(0.8f, 0.42f), new Vector2(0.96f, 0.95f), Vector2.zero, Vector2.zero);
            var feedbackTitle = CreateText("FeedbackTitle", feedbackRoot.rectTransform, "老板反馈", 24, TextAnchor.UpperCenter, new Color(0.96f, 0.97f, 1f));
            Stretch(feedbackTitle.rectTransform, new Vector2(0f, 0.76f), new Vector2(1f, 1f), new Vector2(8f, -6f), new Vector2(-8f, -4f));
            feedbackTitle.raycastTarget = false;
            var feedbackBubble = CreateUiPanel("Bubble", feedbackRoot.rectTransform, new Color(0.18f, 0.2f, 0.28f, 1f));
            Stretch(feedbackBubble.rectTransform, new Vector2(0.08f, 0.2f), new Vector2(0.92f, 0.73f), Vector2.zero, Vector2.zero);
            feedbackBubble.raycastTarget = false;
            var feedbackText = CreateText("FeedbackText", feedbackBubble.rectTransform, "先把版式调顺。", 24, TextAnchor.MiddleCenter, new Color(0.97f, 0.98f, 1f));
            Stretch(feedbackText.rectTransform, new Vector2(0.08f, 0.08f), new Vector2(0.92f, 0.92f), Vector2.zero, Vector2.zero);
            feedbackText.raycastTarget = false;

            var bossFeedback = feedbackRoot.gameObject.AddComponent<BossFeedbackPanel>();
            bossFeedback.Initialize(feedbackText, feedbackBubble);

            var submitButton = CreateButton("SubmitButton", rootRect, "提交给老板");
            Stretch(submitButton.GetComponent<RectTransform>(), new Vector2(0.8f, 0.18f), new Vector2(0.96f, 0.3f), Vector2.zero, Vector2.zero);
            var hintLabel = CreateText("HintLabel", rootRect, "先拖动右侧模块，第一次提交一定会被打回。", 22, TextAnchor.MiddleLeft, new Color(0.86f, 0.88f, 0.94f));
            Stretch(hintLabel.rectTransform, new Vector2(0.05f, 0.03f), new Vector2(0.96f, 0.11f), Vector2.zero, Vector2.zero);
            hintLabel.raycastTarget = false;

            var targetRects = new List<RectTransform>();
            var blocks = new List<DraggableLayoutBlock>();
            CreateLayoutTargetAndBlock(workspace.rectTransform, "标题区", new Vector2(0f, 220f), new Vector2(460f, 90f), new Vector2(-160f, 0f), new Color(0.9f, 0.39f, 0.39f), targetRects, blocks);
            CreateLayoutTargetAndBlock(workspace.rectTransform, "图片框", new Vector2(0f, 40f), new Vector2(380f, 210f), new Vector2(140f, -130f), new Color(0.43f, 0.67f, 0.94f), targetRects, blocks);
            CreateLayoutTargetAndBlock(workspace.rectTransform, "正文区", new Vector2(0f, -150f), new Vector2(440f, 170f), new Vector2(-210f, -230f), new Color(0.54f, 0.8f, 0.6f), targetRects, blocks);
            CreateLayoutTargetAndBlock(workspace.rectTransform, "按钮区", new Vector2(0f, -305f), new Vector2(300f, 80f), new Vector2(180f, 180f), new Color(0.96f, 0.77f, 0.38f), targetRects, blocks);

            var controller = root.AddComponent<LayoutPuzzleController>();
            controller.Initialize(canvasGroup, submitButton, bossFeedback, hintLabel, blocks, targetRects);
            controller.OnSubmitAttempted += coordinator.HandleRightSubmit;
            return controller;
        }

        private static void CreateLayoutTargetAndBlock(
            RectTransform workspace,
            string label,
            Vector2 targetPosition,
            Vector2 size,
            Vector2 blockPosition,
            Color color,
            List<RectTransform> targetRects,
            List<DraggableLayoutBlock> blocks)
        {
            var target = CreateUiPanel(label + "_Target", workspace, new Color(color.r, color.g, color.b, 0.16f));
            target.rectTransform.sizeDelta = size;
            target.rectTransform.anchoredPosition = targetPosition;
            var targetLabel = CreateText("Label", target.rectTransform, label + " 目标位", 18, TextAnchor.MiddleCenter, new Color(color.r * 0.7f, color.g * 0.7f, color.b * 0.7f));
            Stretch(targetLabel.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            targetLabel.raycastTarget = false;

            var block = CreateUiPanel(label + "_Block", workspace, color);
            block.rectTransform.sizeDelta = size;
            block.rectTransform.anchoredPosition = blockPosition;
            var blockLabel = CreateText("Text", block.rectTransform, label, 24, TextAnchor.MiddleCenter, new Color(0.15f, 0.15f, 0.18f));
            Stretch(blockLabel.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            blockLabel.raycastTarget = false;

            var dragBlock = block.gameObject.AddComponent<DraggableLayoutBlock>();
            dragBlock.Initialize(block.rectTransform, target.rectTransform);
            targetRects.Add(target.rectTransform);
            blocks.Add(dragBlock);
        }

        private static Image CreateUiPanel(string name, Transform parent, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var image = go.AddComponent<Image>();
            image.sprite = GetWhiteSprite();
            image.type = Image.Type.Sliced;
            image.color = color;
            return image;
        }

        private static Text CreateText(string name, Transform parent, string content, int fontSize, TextAnchor alignment, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var text = go.AddComponent<Text>();
            text.font = GetRuntimeFont();
            text.text = content;
            text.fontSize = fontSize;
            text.alignment = alignment;
            text.color = color;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            return text;
        }

        private static Button CreateButton(string name, Transform parent, string label)
        {
            var buttonImage = CreateUiPanel(name, parent, new Color(0.4f, 0.7f, 0.97f, 1f));
            var button = buttonImage.gameObject.AddComponent<Button>();
            var colors = button.colors;
            colors.normalColor = new Color(0.4f, 0.7f, 0.97f, 1f);
            colors.highlightedColor = new Color(0.5f, 0.78f, 1f, 1f);
            colors.pressedColor = new Color(0.28f, 0.56f, 0.84f, 1f);
            colors.disabledColor = new Color(0.32f, 0.36f, 0.44f, 0.9f);
            button.colors = colors;

            var labelText = CreateText("Label", buttonImage.rectTransform, label, 26, TextAnchor.MiddleCenter, Color.white);
            Stretch(labelText.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            return button;
        }

        private static SpriteRenderer CreateSpriteObject(string name, Transform parent, Vector2 position, Vector2 size, Color color)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.position = position;
            var renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = GetWhiteSprite();
            renderer.color = color;
            renderer.drawMode = SpriteDrawMode.Sliced;
            renderer.size = size;
            return renderer;
        }

        private static void Stretch(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
        {
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;
            rect.localScale = Vector3.one;
        }

        private static Sprite GetWhiteSprite()
        {
            if (whiteSprite != null)
            {
                return whiteSprite;
            }

            var texture = new Texture2D(1, 1, TextureFormat.RGBA32, false);
            texture.SetPixel(0, 0, Color.white);
            texture.Apply();
            whiteSprite = Sprite.Create(
                texture,
                new Rect(0f, 0f, 1f, 1f),
                new Vector2(0.5f, 0.5f),
                1f,
                0,
                SpriteMeshType.FullRect);
            whiteSprite.name = "RuntimeWhiteSprite";
            return whiteSprite;
        }

        private static Font GetRuntimeFont()
        {
            if (runtimeFont == null)
            {
                runtimeFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            }

            return runtimeFont;
        }

        private static void SetPrivateField<TTarget, TValue>(TTarget target, string fieldName, TValue value)
            where TTarget : class
        {
            var field = typeof(TTarget).GetField(fieldName, System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (field != null)
            {
                field.SetValue(target, value);
            }
        }

        private readonly struct LeftWorldReferences
        {
            public LeftWorldReferences(LeftWorldActivationController activationController, DreamGoalTrigger goalTrigger)
            {
                ActivationController = activationController;
                GoalTrigger = goalTrigger;
            }

            public LeftWorldActivationController ActivationController { get; }
            public DreamGoalTrigger GoalTrigger { get; }
        }

        private readonly struct ScreenUiReferences
        {
            public ScreenUiReferences(
                RectTransform leftFrame,
                RectTransform rightFrame,
                Image rightMask,
                Text rightStateText,
                Image leftMask,
                Text leftStateText,
                Text stageLabel,
                Text completionLabel)
            {
                LeftFrame = leftFrame;
                RightFrame = rightFrame;
                RightMask = rightMask;
                RightStateText = rightStateText;
                LeftMask = leftMask;
                LeftStateText = leftStateText;
                StageLabel = stageLabel;
                CompletionLabel = completionLabel;
            }

            public RectTransform LeftFrame { get; }
            public RectTransform RightFrame { get; }
            public Image RightMask { get; }
            public Text RightStateText { get; }
            public Image LeftMask { get; }
            public Text LeftStateText { get; }
            public Text StageLabel { get; }
            public Text CompletionLabel { get; }
        }
    }
}
