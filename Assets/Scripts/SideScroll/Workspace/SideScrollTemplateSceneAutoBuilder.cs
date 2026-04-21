using System.Reflection;
using UnityEngine;

namespace GameCreate3
{
    public static class SideScrollTemplateSceneAutoBuilder
    {
        public static void BuildStoryTemplate()
        {
            if (Object.FindObjectOfType<SideScrollWorkspaceBase>() != null)
            {
                return;
            }

            var workspace = BuildWorkspaceRoot<SideScrollStoryWorkspace>("WorkspaceRoot");
            var spawn = CreateNamedChild(workspace.transform, "PlayerSpawn", new Vector3(-4f, -1.5f, 0f));
            var player = BuildSharedPlayer(workspace.transform, spawn.transform.position);
            var environment = CreateNamedChild(workspace.transform, "Environment", Vector3.zero);
            CreateGround(environment.transform, new Vector2(0f, -2.4f), new Vector2(18f, 1f), new Color(0.22f, 0.28f, 0.34f));
            var bounds = BuildCameraBounds(environment.transform);
            SideScrollTestWorkspaceAutoBuilder.BuildIfNeeded();
            Object.DestroyImmediate(Object.FindObjectOfType<SideScrollStoryWorkspace>()?.gameObject);

            BuildCameraRig(workspace.transform, player.transform, bounds);
            BuildObservation(workspace.transform);
            BuildDialogueTrigger(workspace.transform);
            BuildCameraZone(workspace.transform);
            BuildExit(workspace.transform);
            workspace.Initialize();
        }

        public static void BuildGameplayTemplate()
        {
            if (Object.FindObjectOfType<SideScrollWorkspaceBase>() != null)
            {
                return;
            }

            var workspace = BuildWorkspaceRoot<SideScrollGameplayWorkspace>("WorkspaceRoot");
            var spawn = CreateNamedChild(workspace.transform, "PlayerSpawn", new Vector3(-4f, -1.5f, 0f));
            var player = BuildSharedPlayer(workspace.transform, spawn.transform.position);
            var environment = CreateNamedChild(workspace.transform, "Environment", Vector3.zero);
            CreateGround(environment.transform, new Vector2(0f, -2.4f), new Vector2(18f, 1f), new Color(0.24f, 0.31f, 0.36f));
            var bounds = BuildCameraBounds(environment.transform);
            BuildCameraRig(workspace.transform, player.transform, bounds);
            BuildPickup(workspace.transform);
            BuildPushable(workspace.transform);
            BuildGoalTrigger(workspace.transform);
            BuildExit(workspace.transform);
            workspace.Initialize();
        }

        private static T BuildWorkspaceRoot<T>(string name) where T : SideScrollWorkspaceBase
        {
            var root = new GameObject(name);
            return root.AddComponent<T>();
        }

        private static GameObject BuildSharedPlayer(Transform parent, Vector3 position)
        {
            var player = new GameObject("SideScrollPlayer");
            player.transform.SetParent(parent, false);
            player.transform.position = position;
            player.layer = GetLayerOrDefault("Player", 0);
            AddSpriteAndBox(player, new Vector2(0.8f, 1.4f), new Color(0.9f, 0.62f, 0.35f), false);
            var body = player.AddComponent<Rigidbody2D>();
            body.freezeRotation = true;
            body.gravityScale = 3f;

            player.AddComponent<CharacterInputProxy>();
            var ground = player.AddComponent<CharacterGroundDetector>();
            player.AddComponent<CharacterMovementMotor>();
            player.AddComponent<CharacterJumpMotor>();
            player.AddComponent<SideScrollInteractionDetector>();
            var controller = player.AddComponent<SideScrollCharacterControllerBase>();

            var point = new GameObject("GroundCheck");
            point.transform.SetParent(player.transform, false);
            point.transform.localPosition = new Vector3(0f, -0.75f, 0f);
            SetPrivateField(ground, "groundCheckPoint", point.transform);
            SetPrivateField(ground, "groundMask", (LayerMask)LayerMask.GetMask("Ground"));
            SetPrivateField(player.GetComponent<SideScrollInteractionDetector>(), "interactableMask", (LayerMask)LayerMask.GetMask("Interactable"));
            controller.ApplyConfigs(CreateMoveConfig(), CreateJumpConfig(), (LayerMask)LayerMask.GetMask("Ground"));
            return player;
        }

        private static void BuildCameraRig(Transform parent, Transform followTarget, Collider2D bounds)
        {
            SideScrollTestWorkspaceAutoBuilder.BuildIfNeeded();
            Object.DestroyImmediate(Object.FindObjectOfType<SideScrollStoryWorkspace>()?.gameObject);
            var rig = new GameObject("CameraRig");
            rig.transform.SetParent(parent, false);
            var vcamObject = new GameObject("CM_VCam");
            vcamObject.transform.SetParent(rig.transform, false);
            var vcam = vcamObject.AddComponent<Cinemachine.CinemachineVirtualCamera>();
            vcam.m_Lens.Orthographic = true;
            vcam.Follow = followTarget;
            vcam.AddCinemachineComponent<Cinemachine.CinemachineFramingTransposer>();
            var confiner = vcamObject.AddComponent<Cinemachine.CinemachineConfiner2D>();
            confiner.m_BoundingShape2D = bounds;
            var controller = rig.AddComponent<SideScrollCameraController>();
            SetPrivateField(controller, "virtualCamera", vcam);
            SetPrivateField(controller, "confiner2D", confiner);
            controller.ApplyCameraConfig(CreateDefaultCameraConfig());
        }

        private static void BuildObservation(Transform parent)
        {
            var root = CreateNamedChild(parent, "Interactables", Vector3.zero);
            var point = CreateNamedChild(root.transform, "ObservationPoint", new Vector3(-1f, -1.5f, 0f));
            point.layer = GetLayerOrDefault("Interactable", 0);
            AddSpriteAndBox(point, new Vector2(0.8f, 0.8f), new Color(0.33f, 0.82f, 0.72f), true);
            point.AddComponent<ObservationPoint>();
        }

        private static void BuildPickup(Transform parent)
        {
            var root = CreateNamedChild(parent, "Interactables", Vector3.zero);
            var pickup = CreateNamedChild(root.transform, "PickupObject", new Vector3(-1.5f, -1.4f, 0f));
            pickup.layer = GetLayerOrDefault("Interactable", 0);
            AddSpriteAndBox(pickup, new Vector2(0.6f, 0.6f), new Color(0.95f, 0.84f, 0.36f), true);
            pickup.AddComponent<PickupObject>();
        }

        private static void BuildPushable(Transform parent)
        {
            var root = CreateNamedChild(parent, "Interactables", Vector3.zero);
            var pushable = CreateNamedChild(root.transform, "PushableObject", new Vector3(1f, -1.4f, 0f));
            pushable.layer = GetLayerOrDefault("Interactable", 0);
            AddSpriteAndBox(pushable, new Vector2(1f, 1f), new Color(0.65f, 0.54f, 0.4f), false);
            var body = pushable.AddComponent<Rigidbody2D>();
            body.gravityScale = 0f;
            body.freezeRotation = true;
            pushable.AddComponent<PushableObject>();
        }

        private static void BuildExit(Transform parent)
        {
            var root = parent.Find("Interactables") ?? CreateNamedChild(parent, "Interactables", Vector3.zero).transform;
            var exit = CreateNamedChild(root, "ExitPoint", new Vector3(5f, -1.2f, 0f));
            exit.layer = GetLayerOrDefault("Interactable", 0);
            AddSpriteAndBox(exit, new Vector2(1.2f, 1.8f), new Color(0.45f, 0.62f, 0.95f), true);
            exit.AddComponent<ExitPoint>();
        }

        private static void BuildDialogueTrigger(Transform parent)
        {
            var root = CreateNamedChild(parent, "Triggers", Vector3.zero);
            var trigger = CreateNamedChild(root.transform, "Trigger_Dialogue", new Vector3(1.5f, -1.2f, 0f));
            trigger.layer = GetLayerOrDefault("Trigger", 0);
            AddSpriteAndBox(trigger, new Vector2(1.2f, 1.8f), new Color(1f, 1f, 1f, 0.1f), true);
            trigger.AddComponent<DialogueTriggerZone>();
        }

        private static void BuildGoalTrigger(Transform parent)
        {
            var root = CreateNamedChild(parent, "Triggers", Vector3.zero);
            var trigger = CreateNamedChild(root.transform, "Trigger_Goal", new Vector3(3.5f, -1.2f, 0f));
            trigger.layer = GetLayerOrDefault("Trigger", 0);
            AddSpriteAndBox(trigger, new Vector2(1.2f, 1.8f), new Color(1f, 1f, 1f, 0.1f), true);
            trigger.AddComponent<GoalTriggerZone>();
        }

        private static void BuildCameraZone(Transform parent)
        {
            var root = CreateNamedChild(parent, "CameraZones", Vector3.zero);
            var zone = CreateNamedChild(root.transform, "CameraZone", new Vector3(2.5f, -1.2f, 0f));
            zone.layer = GetLayerOrDefault("Trigger", 0);
            AddSpriteAndBox(zone, new Vector2(2f, 2f), new Color(0.7f, 0.9f, 1f, 0.08f), true);
            var cameraZone = zone.AddComponent<CameraZone>();
            SetPrivateField(cameraZone, "overrideConfig", CreateCloseupCameraConfig());
        }

        private static Collider2D BuildCameraBounds(Transform parent)
        {
            var bounds = new GameObject("CameraBounds");
            bounds.transform.SetParent(parent, false);
            bounds.transform.position = new Vector3(2f, 1f, 0f);
            var collider2D = bounds.AddComponent<BoxCollider2D>();
            collider2D.size = new Vector2(18f, 10f);
            collider2D.isTrigger = true;
            return collider2D;
        }

        private static void CreateGround(Transform parent, Vector2 position, Vector2 size, Color color)
        {
            var ground = new GameObject("Ground");
            ground.transform.SetParent(parent, false);
            ground.transform.position = position;
            ground.layer = GetLayerOrDefault("Ground", 0);
            AddSpriteAndBox(ground, size, color, false);
        }

        private static GameObject CreateNamedChild(Transform parent, string name, Vector3 position)
        {
            var child = new GameObject(name);
            child.transform.SetParent(parent, false);
            child.transform.position = position;
            return child;
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

        private static CameraConfig CreateCloseupCameraConfig()
        {
            var config = ScriptableObject.CreateInstance<CameraConfig>();
            config.followOffset = new Vector3(0.4f, 1.35f, -10f);
            config.damping = new Vector2(0.12f, 0.12f);
            config.orthographicSize = 4.2f;
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
