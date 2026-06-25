using System.Collections;
using GameCreate3.Core.SceneRouting;
using UnityEngine;

namespace GameCreate3.Level3
{
    /// <summary>
    /// 代码版事件路由：订阅 Workspace 事件并分发到各模块。
    /// 替代在 Inspector 手配 20+ 条 WorkspaceEventRouter binding。
    /// </summary>
    public sealed class Level3EventBridge : MonoBehaviour
    {
        [Header("Modules")]
        [SerializeField] private BossAttackSpawner bossAttackSpawner;
        [SerializeField] private InvisibleWallController wallController;
        [SerializeField] private FollowerCounter followerCounter;
        [SerializeField] private ClimaxSequenceController climaxSequence;
        [SerializeField] private Level3PhaseController phaseController;
        [SerializeField] private ParryController parryController;

        [Header("Transition")]
        [SerializeField] private Level3DualWorldTransition dualWorldTransition;

        [Header("UI (assign after Canvas is set up)")]
        [SerializeField] private Level3XiaohongshuUI xiaohongshuUI;
        [SerializeField] private Level3HUD hud;
        [SerializeField] private ParryTutorialPopup parryTutorialPopup;

        [Header("VFX")]
        [SerializeField] private FollowerBubbleSpawner followerBubbleSpawner;

        [Header("Transition (通关白屏)")]
        [SerializeField] private ScreenWhiteout screenWhiteout;
        [SerializeField] private float victoryWhiteoutDelay = 1f;
        [SerializeField] private string victoryRouteId = "level3cutscene";

        private SideScrollWorkspaceBase workspace;

        private void Awake()
        {
            workspace = GetComponentInParent<SideScrollWorkspaceBase>(true);
            if (workspace == null)
            {
                Debug.LogWarning("[Level3EventBridge] SideScrollWorkspaceBase not found.", this);
                return;
            }
            workspace.WorkspaceEventRaised += OnEvent;
            AutoResolve();
            if (followerCounter != null && followerBubbleSpawner != null)
                followerCounter.FollowerDeltaOccurred += followerBubbleSpawner.SpawnBubbles;
        }

        private void OnDestroy()
        {
            if (workspace != null)
                workspace.WorkspaceEventRaised -= OnEvent;
            if (followerCounter != null && followerBubbleSpawner != null)
                followerCounter.FollowerDeltaOccurred -= followerBubbleSpawner.SpawnBubbles;
        }

        private void OnEvent(string eventId)
        {
            switch (eventId)
            {
                case Level3Events.Phase1:
                    bossAttackSpawner?.OnPhase1();
                    wallController?.StartMoving();
                    hud?.OnPhase1();
                    break;

                case Level3Events.Phase2:
                    bossAttackSpawner?.OnPhase2();
                    wallController?.StopMoving();
                    followerCounter?.OnPhase2();
                    parryController?.OnPhase2();
                    dualWorldTransition?.OnPhase2();
                    xiaohongshuUI?.OnPhase2();
                    hud?.OnPhase2();
                    followerBubbleSpawner?.SetAutoActive(true);
                    parryTutorialPopup?.Show();
                    break;

                case Level3Events.Phase3:
                    parryController?.OnPhase3();
                    hud?.OnPhase3();
                    break;

                case Level3Events.PlayerHit:
                    wallController?.AccelerateOnHit();
                    followerCounter?.OnPlayerHit();
                    hud?.OnPlayerHit();
                    break;

                case Level3Events.PlayerDefeated:
                    phaseController?.OnPlayerDefeated();
                    break;

                case Level3Events.ParrySuccess:
                    followerCounter?.OnParrySuccess();
                    wallController?.PushBack();
                    break;

                case Level3Events.FollowerChanged:
                    xiaohongshuUI?.OnFollowerChanged();
                    break;

                case Level3Events.Follower2000:
                    climaxSequence?.OnFollowerThreshold();
                    xiaohongshuUI?.OnComment1();
                    break;

                case Level3Events.Follower4000:
                    climaxSequence?.OnFollowerThreshold();
                    xiaohongshuUI?.OnComment2();
                    break;

                case Level3Events.Follower6000:
                    climaxSequence?.OnFollowerThreshold();
                    xiaohongshuUI?.OnComment3();
                    break;

                case Level3Events.Follower8000:
                    climaxSequence?.OnFollowerThreshold();
                    phaseController?.OnFollowerThresholdMax();
                    xiaohongshuUI?.OnComment4();
                    break;

                case Level3Events.SequenceCorrect:
                    xiaohongshuUI?.OnSequenceCorrect();
                    break;

                case Level3Events.SequenceWrong:
                    xiaohongshuUI?.OnSequenceWrong();
                    break;

                case Level3Events.SequenceComplete:
                    phaseController?.OnSequenceComplete();
                    followerCounter?.OnSequenceComplete();
                    bossAttackSpawner?.OnSequenceComplete();
                    wallController?.ReturnToStart();
                    break;

                case Level3Events.LevelComplete:
                    bossAttackSpawner?.OnPhase3();
                    xiaohongshuUI?.OnLevelComplete();
                    hud?.OnLevelEnd();
                    break;

                case Level3Events.LevelFail:
                    hud?.OnLevelEnd();
                    SceneRouter.Reload();
                    break;
            }
        }

        private void AutoResolve()
        {
            if (bossAttackSpawner == null)
                bossAttackSpawner = FindObjectOfType<BossAttackSpawner>(true);
            if (wallController == null)
                wallController = GetComponentInParent<InvisibleWallController>(true);
            if (followerCounter == null)
                followerCounter = GetComponentInParent<FollowerCounter>(true);
            if (climaxSequence == null)
                climaxSequence = GetComponentInParent<ClimaxSequenceController>(true);
            if (phaseController == null)
                phaseController = GetComponentInParent<Level3PhaseController>(true);
            if (parryController == null)
                parryController = FindObjectOfType<ParryController>(true);
            if (dualWorldTransition == null)
                dualWorldTransition = GetComponentInParent<Level3DualWorldTransition>(true);
            if (xiaohongshuUI == null)
                xiaohongshuUI = FindObjectOfType<Level3XiaohongshuUI>(true);
            if (hud == null)
                hud = FindObjectOfType<Level3HUD>(true);
            if (screenWhiteout == null)
                screenWhiteout = FindObjectOfType<ScreenWhiteout>(true);
            if (followerBubbleSpawner == null)
                followerBubbleSpawner = FindObjectOfType<FollowerBubbleSpawner>(true);
            if (parryTutorialPopup == null)
                parryTutorialPopup = FindObjectOfType<ParryTutorialPopup>(true);
        }

        public void TriggerVictoryTransition()
        {
            StartCoroutine(VictoryTransition());
        }

        private IEnumerator VictoryTransition()
        {
            if (screenWhiteout != null)
                screenWhiteout.Trigger();

            yield return new WaitForSeconds(victoryWhiteoutDelay);

            if (!string.IsNullOrEmpty(victoryRouteId))
                SceneRouter.Go(victoryRouteId);
            else
                SceneRouter.GoScene("Level3Cutscene");
        }
    }
}
