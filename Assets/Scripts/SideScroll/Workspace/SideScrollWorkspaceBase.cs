using System;
using System.Collections.Generic;
using UnityEngine;

namespace GameCreate3
{
    public class SideScrollWorkspaceBase : MonoBehaviour
    {
        [SerializeField] private SideScrollWorkspaceConfig config;
        [SerializeField] private Transform playerSpawn;
        [SerializeField] private SideScrollCharacterControllerBase playerController;
        [SerializeField] private SideScrollCameraController cameraController;
        [SerializeField] private LayerMask groundMask;
        [SerializeField] private LayerMask interactableMask;

        private readonly HashSet<string> raisedEventIds = new HashSet<string>();
        private readonly HashSet<string> collectedPickupIds = new HashSet<string>();
        private readonly HashSet<string> completedGoalIds = new HashSet<string>();
        private readonly List<ISideScrollInteractable> registeredInteractables = new List<ISideScrollInteractable>();
        private readonly List<TriggerZoneBase> registeredTriggers = new List<TriggerZoneBase>();
        private readonly List<CameraZone> registeredCameraZones = new List<CameraZone>();

        private bool initialized;

        public event Action<string> WorkspaceEventRaised;

        public SideScrollCharacterControllerBase PlayerController => playerController;
        public SideScrollCameraController CameraController => cameraController;
        public bool IsEntered { get; private set; }

        protected virtual void Awake()
        {
            Initialize();
        }

        protected virtual void Start()
        {
            Enter();
        }

        public virtual void Initialize()
        {
            if (initialized)
            {
                return;
            }

            ResolveReferences();
            ApplyConfig();
            ScanSceneObjects();
            RegisterSceneObjects();
            BindWorkspaceEvents();
            initialized = true;
        }

        public virtual void Enter()
        {
            if (IsEntered)
            {
                return;
            }

            IsEntered = true;
            playerController?.SetInputEnabled(config == null || config.inputEnabledOnEnter);
            OnWorkspaceEntered();
        }

        public virtual void Exit()
        {
            if (!IsEntered)
            {
                return;
            }

            IsEntered = false;
            playerController?.SetInputEnabled(false);
            OnWorkspaceExited();
        }

        public virtual void Pause()
        {
            playerController?.SetInputEnabled(false);
        }

        public virtual void Resume()
        {
            playerController?.SetInputEnabled(true);
        }

        public void RaiseWorkspaceEvent(string eventId)
        {
            if (string.IsNullOrWhiteSpace(eventId))
            {
                return;
            }

            raisedEventIds.Add(eventId);
            WorkspaceEventRaised?.Invoke(eventId);
        }

        public bool HasWorkspaceEvent(string eventId)
        {
            return !string.IsNullOrWhiteSpace(eventId) && raisedEventIds.Contains(eventId);
        }

        public void RegisterPickup(string pickupId)
        {
            if (!string.IsNullOrWhiteSpace(pickupId))
            {
                collectedPickupIds.Add(pickupId);
            }
        }

        public bool HasPickup(string pickupId)
        {
            return !string.IsNullOrWhiteSpace(pickupId) && collectedPickupIds.Contains(pickupId);
        }

        public void RegisterGoal(string goalId)
        {
            if (!string.IsNullOrWhiteSpace(goalId))
            {
                completedGoalIds.Add(goalId);
            }
        }

        public bool HasGoal(string goalId)
        {
            return !string.IsNullOrWhiteSpace(goalId) && completedGoalIds.Contains(goalId);
        }

        public bool EvaluateRequirements(IReadOnlyList<ConditionRequirementData> requirements)
        {
            if (requirements == null || requirements.Count == 0)
            {
                return true;
            }

            for (var i = 0; i < requirements.Count; i++)
            {
                var requirement = requirements[i];
                var passed = requirement.kind switch
                {
                    ConditionRequirementData.RequirementKind.Pickup => HasPickup(requirement.id),
                    ConditionRequirementData.RequirementKind.Goal => HasGoal(requirement.id),
                    _ => HasWorkspaceEvent(requirement.id)
                };

                if (requirement.invert)
                {
                    passed = !passed;
                }

                if (!passed)
                {
                    return false;
                }
            }

            return true;
        }

        public bool EvaluateConfiguredCompletion()
        {
            if (config == null)
            {
                return true;
            }

            return EvaluateIds(config.requiredEventIds, HasWorkspaceEvent) &&
                EvaluateIds(config.requiredPickupIds, HasPickup) &&
                EvaluateIds(config.requiredGoalIds, HasGoal);
        }

        protected virtual void RegisterSceneObjects()
        {
        }

        protected virtual void BindWorkspaceEvents()
        {
        }

        protected virtual void OnWorkspaceEntered()
        {
        }

        protected virtual void OnWorkspaceExited()
        {
        }

        private void ResolveReferences()
        {
            playerController = playerController != null ? playerController : GetComponentInChildren<SideScrollCharacterControllerBase>(true);
            cameraController = cameraController != null ? cameraController : GetComponentInChildren<SideScrollCameraController>(true);
            playerSpawn = playerSpawn != null ? playerSpawn : transform.Find("PlayerSpawn");
        }

        private void ApplyConfig()
        {
            if (playerController != null && config != null)
            {
                playerController.ApplyConfigs(config.moveConfig, config.jumpConfig, groundMask == 0 ? ~0 : groundMask);
            }

            // SetFollowTarget 必须独立于 config 是否为 null —— prefab 化后跨 prefab 引用会被切断，
            // vcam.Follow 落库时是 null，必须在运行时由 workspace 修复。
            if (cameraController != null)
            {
                cameraController.SetFollowTarget(playerController != null ? playerController.transform : null);
                cameraController.EnsureConfinerBinding();
                if (config != null && config.defaultCameraConfig != null)
                {
                    cameraController.ApplyCameraConfig(config.defaultCameraConfig);
                }
                else
                {
                    cameraController.ResetToDefault();
                }
            }

            if (playerController != null && playerSpawn != null)
            {
                playerController.transform.position = playerSpawn.position;
            }

            if (playerController != null && playerController.TryGetComponent<SideScrollInteractionDetector>(out var detector))
            {
                detector.SetInteractableMask(interactableMask == 0 ? ~0 : interactableMask);
            }
        }

        private void ScanSceneObjects()
        {
            registeredInteractables.Clear();
            registeredTriggers.Clear();
            registeredCameraZones.Clear();

            foreach (var behaviour in GetComponentsInChildren<MonoBehaviour>(true))
            {
                if (behaviour is ISideScrollInteractable interactable)
                {
                    registeredInteractables.Add(interactable);
                    if (behaviour is SideScrollInteractableBase interactableBase)
                    {
                        interactableBase.BindWorkspace(this);
                    }
                }

                if (behaviour is TriggerZoneBase trigger)
                {
                    registeredTriggers.Add(trigger);
                    trigger.BindWorkspace(this);
                }

                if (behaviour is CameraZone cameraZone)
                {
                    registeredCameraZones.Add(cameraZone);
                    cameraZone.BindWorkspace(this);
                }
            }
        }

        private static bool EvaluateIds(IReadOnlyList<string> ids, Func<string, bool> predicate)
        {
            if (ids == null)
            {
                return true;
            }

            for (var i = 0; i < ids.Count; i++)
            {
                var id = ids[i];
                if (!string.IsNullOrWhiteSpace(id) && !predicate(id))
                {
                    return false;
                }
            }

            return true;
        }
    }
}
