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
        [SerializeField] private Level3FailurePage failurePage;
        [SerializeField] private Level3VictoryOverlay victoryOverlay;

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
        }

        private void OnDestroy()
        {
            if (workspace != null)
                workspace.WorkspaceEventRaised -= OnEvent;
        }

        private void OnEvent(string eventId)
        {
            switch (eventId)
            {
                case Level3Events.Phase1:
                    bossAttackSpawner?.OnPhase1();
                    wallController?.OnPhase1();
                    hud?.OnPhase1();
                    break;

                case Level3Events.Phase2:
                    bossAttackSpawner?.OnPhase2();
                    wallController?.OnPhase2();
                    followerCounter?.OnPhase2();
                    parryController?.OnPhase2();
                    dualWorldTransition?.OnPhase2();
                    xiaohongshuUI?.OnPhase2();
                    hud?.OnPhase2();
                    break;

                case Level3Events.Phase3:
                    bossAttackSpawner?.OnPhase3();
                    parryController?.OnPhase3();
                    hud?.OnPhase3();
                    break;

                case Level3Events.PlayerHit:
                    wallController?.OnPlayerHit();
                    followerCounter?.OnPlayerHit();
                    hud?.OnPlayerHit();
                    break;

                case Level3Events.PlayerDefeated:
                    phaseController?.OnPlayerDefeated();
                    break;

                case Level3Events.ParrySuccess:
                    followerCounter?.OnParrySuccess();
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
                    xiaohongshuUI?.OnSequenceComplete();
                    break;

                case Level3Events.LevelComplete:
                    victoryOverlay?.OnLevelComplete();
                    xiaohongshuUI?.OnLevelComplete();
                    hud?.OnLevelEnd();
                    break;

                case Level3Events.LevelFail:
                    failurePage?.OnLevelFail();
                    hud?.OnLevelEnd();
                    break;
            }
        }

        private void AutoResolve()
        {
            if (bossAttackSpawner == null)
                bossAttackSpawner = GetComponentInParent<BossAttackSpawner>(true);
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
            if (failurePage == null)
                failurePage = FindObjectOfType<Level3FailurePage>(true);
            if (victoryOverlay == null)
                victoryOverlay = FindObjectOfType<Level3VictoryOverlay>(true);
        }
    }
}
