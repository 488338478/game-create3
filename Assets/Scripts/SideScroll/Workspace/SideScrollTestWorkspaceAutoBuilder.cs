using System.Reflection;
using Cinemachine;
using UnityEngine;

namespace GameCreate3
{
    public static class SideScrollTestWorkspaceAutoBuilder
    {
        public static void BuildIfNeeded()
        {
            if (Object.FindObjectOfType<SideScrollWorkspaceBase>() != null)
            {
                return;
            }

            var root = new GameObject("WorkspaceRoot");
            var workspace = root.AddComponent<SideScrollStoryWorkspace>();

            var playerSpawn = new GameObject("PlayerSpawn");
            playerSpawn.transform.SetParent(root.transform, false);
            playerSpawn.transform.position = new Vector3(-4f, -1.5f, 0f);

            var player = BuildPlayer(root.transform, playerSpawn.transform.position);
            var environmentRoot = new GameObject("Environment");
            environmentRoot.transform.SetParent(root.transform, false);

            CreateGround(environmentRoot.transform, new Vector2(0f, -2.4f), new Vector2(18f, 1f), new Color(0.25f, 0.3f, 0.35f));
            var confiner = BuildCameraBounds(environmentRoot.transform);
            BuildCameraRig(root.transform, player.transform, confiner);
            BuildInteractable(root.transform);
            BuildTrigger(root.transform);
            workspace.Initialize();
        }

        private static GameObject BuildPlayer(Transform parent, Vector3 position)
        {
            var player = new GameObject("SideScrollPlayer");
            player.transform.SetParent(parent, false);
            player.transform.position = position;
            player.layer = GetLayerOrDefault("Player", 0);

            AddSpriteAndBox(player, new Vector2(0.8f, 1.4f), new Color(0.85f, 0.55f, 0.3f), false);
            var body = player.AddComponent<Rigidbody2D>();
            body.freezeRotation = true;
            body.gravityScale = 3f;

            var controller = player.AddComponent<SideScrollCharacterControllerBase>();
            player.AddComponent<CharacterInputProxy>();
            player.AddComponent<CharacterGroundDetector>();
            player.AddComponent<CharacterMovementMotor>();
            player.AddComponent<CharacterJumpMotor>();
            player.AddComponent<SideScrollInteractionDetector>();

            var scan = new GameObject("GroundCheck");
            scan.transform.SetParent(player.transform, false);
            scan.transform.localPosition = new Vector3(0f, -0.75f, 0f);
            SetPrivateField(player.GetComponent<CharacterGroundDetector>(), "groundCheckPoint", scan.transform);
            SetPrivateField(player.GetComponent<CharacterGroundDetector>(), "groundMask", (LayerMask)LayerMask.GetMask("Ground"));
            SetPrivateField(player.GetComponent<SideScrollInteractionDetector>(), "interactableMask", (LayerMask)LayerMask.GetMask("Interactable"));
            controller.ApplyConfigs(CreateMoveConfig(), CreateJumpConfig(), (LayerMask)LayerMask.GetMask("Ground"));
            return player;
        }

        private static void BuildCameraRig(Transform parent, Transform followTarget, Collider2D confinerShape)
        {
            var rig = new GameObject("CameraRig");
            rig.transform.SetParent(parent, false);

            var mainCamera = Camera.main;
            if (mainCamera == null)
            {
                var cameraObject = new GameObject("Main Camera");
                cameraObject.tag = "MainCamera";
                cameraObject.transform.position = new Vector3(0f, 0f, -10f);
                cameraObject.AddComponent<AudioListener>();
                mainCamera = cameraObject.AddComponent<Camera>();
                mainCamera.orthographic = true;
                mainCamera.orthographicSize = 5f;
            }

            var brain = mainCamera.GetComponent<CinemachineBrain>();
            if (brain == null)
            {
                brain = mainCamera.gameObject.AddComponent<CinemachineBrain>();
            }

            var vcamObject = new GameObject("CM_VCam");
            vcamObject.transform.SetParent(rig.transform, false);
            var vcam = vcamObject.AddComponent<CinemachineVirtualCamera>();
            vcam.m_Lens.Orthographic = true;
            vcam.Follow = followTarget;
            vcam.AddCinemachineComponent<CinemachineFramingTransposer>();
            var confiner = vcamObject.AddComponent<CinemachineConfiner2D>();
            confiner.m_BoundingShape2D = confinerShape;

            var controller = rig.AddComponent<SideScrollCameraController>();
            SetPrivateField(controller, "virtualCamera", vcam);
            SetPrivateField(controller, "confiner2D", confiner);
            controller.ApplyCameraConfig(CreateDefaultCameraConfig());
        }

        private static void BuildInteractable(Transform parent)
        {
            var root = new GameObject("Interactables");
            root.transform.SetParent(parent, false);

            var interactable = new GameObject("ObservationPoint");
            interactable.transform.SetParent(root.transform, false);
            interactable.transform.position = new Vector3(-1f, -1.5f, 0f);
            interactable.layer = GetLayerOrDefault("Interactable", 0);
            AddSpriteAndBox(interactable, new Vector2(0.8f, 0.8f), new Color(0.35f, 0.8f, 0.7f), true);
            interactable.AddComponent<ObservationPoint>();
        }

        private static void BuildTrigger(Transform parent)
        {
            var triggerRoot = new GameObject("Triggers");
            triggerRoot.transform.SetParent(parent, false);

            var trigger = new GameObject("WorkspaceEventTrigger");
            trigger.transform.SetParent(triggerRoot.transform, false);
            trigger.transform.position = new Vector3(2f, -1.2f, 0f);
            trigger.layer = GetLayerOrDefault("Trigger", 0);
            AddSpriteAndBox(trigger, new Vector2(1.2f, 1.8f), new Color(1f, 1f, 1f, 0.1f), true);
            trigger.AddComponent<WorkspaceEventTriggerZone>();
        }

        private static Collider2D BuildCameraBounds(Transform parent)
        {
            var bounds = new GameObject("CameraBounds");
            bounds.transform.SetParent(parent, false);
            bounds.transform.position = new Vector3(2f, 1f, 0f);
            var collider2D = bounds.AddComponent<PolygonCollider2D>();
            collider2D.SetPath(0, CreateRectPath(new Vector2(18f, 10f)));
            collider2D.isTrigger = true;
            return collider2D;
        }

        private static Vector2[] CreateRectPath(Vector2 size)
        {
            var half = size * 0.5f;
            return new[]
            {
                new Vector2(-half.x, -half.y),
                new Vector2(-half.x, half.y),
                new Vector2(half.x, half.y),
                new Vector2(half.x, -half.y)
            };
        }

        private static void CreateGround(Transform parent, Vector2 position, Vector2 size, Color color)
        {
            var ground = new GameObject("Ground");
            ground.transform.SetParent(parent, false);
            ground.transform.position = position;
            ground.layer = GetLayerOrDefault("Ground", 0);
            AddSpriteAndBox(ground, size, color, false);
        }

        private static void AddSpriteAndBox(GameObject target, Vector2 size, Color color, bool isTrigger)
        {
            var renderer = target.AddComponent<SpriteRenderer>();
            renderer.sprite = CreateSprite();
            renderer.color = color;
            renderer.drawMode = SpriteDrawMode.Sliced;
            renderer.size = size;

            var box = target.AddComponent<BoxCollider2D>();
            box.size = size;
            box.isTrigger = isTrigger;
        }

        private static Sprite CreateSprite()
        {
            var texture = new Texture2D(1, 1);
            texture.SetPixel(0, 0, Color.white);
            texture.Apply();
            return Sprite.Create(texture, new Rect(0f, 0f, 1f, 1f), new Vector2(0.5f, 0.5f), 1f, 0u, SpriteMeshType.FullRect);
        }

        private static CameraConfig CreateDefaultCameraConfig()
        {
            var config = ScriptableObject.CreateInstance<CameraConfig>();
            config.followOffset = new Vector3(0f, 1.2f, -10f);
            config.damping = new Vector2(0.2f, 0.2f);
            config.orthographicSize = 5f;
            return config;
        }

        private static CharacterMoveConfig CreateMoveConfig()
        {
            return ScriptableObject.CreateInstance<CharacterMoveConfig>();
        }

        private static CharacterJumpConfig CreateJumpConfig()
        {
            return ScriptableObject.CreateInstance<CharacterJumpConfig>();
        }

        private static int GetLayerOrDefault(string layerName, int fallback)
        {
            var layer = LayerMask.NameToLayer(layerName);
            return layer >= 0 ? layer : fallback;
        }

        private static void SetPrivateField<TTarget, TValue>(TTarget target, string fieldName, TValue value)
        {
            var field = typeof(TTarget).GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            if (field != null)
            {
                field.SetValue(target, value);
            }
        }
    }
}
