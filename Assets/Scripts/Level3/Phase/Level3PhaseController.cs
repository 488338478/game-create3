using UnityEngine;

namespace GameCreate3.Level3
{
    public sealed class Level3PhaseController : MonoBehaviour
    {
        public enum Phase { None, Phase1_Dodge, Phase2_DualWorld, Phase3_Climax, LevelComplete, GameOver }

        [Header("Timing")]
        [SerializeField] private float phase1Duration = 15f;

        public Phase CurrentPhase { get; private set; } = Phase.None;

        private SideScrollWorkspaceBase workspace;
        private float phase1Timer;
        private bool phase1Complete;

        private void Awake()
        {
            workspace = GetComponentInParent<SideScrollWorkspaceBase>(true);
        }

        private void Update()
        {
            if (CurrentPhase == Phase.Phase1_Dodge && !phase1Complete)
            {
                phase1Timer += Time.deltaTime;
                if (phase1Timer >= phase1Duration)
                {
                    phase1Complete = true;
                    EnterPhase(Phase.Phase2_DualWorld);
                }
            }
        }

        public void Begin()
        {
            EnterPhase(Phase.Phase1_Dodge);
        }

        // --- WorkspaceEventRouter 调用的 public 入口 ---

        public void OnFollowerThresholdMax()
        {
            if (CurrentPhase == Phase.Phase2_DualWorld)
                EnterPhase(Phase.Phase3_Climax);
        }

        public void OnSequenceComplete()
        {
            EnterPhase(Phase.LevelComplete);
        }

        public void OnPlayerDefeated()
        {
            EnterPhase(Phase.GameOver);
        }

        // --- 内部 ---

        private void EnterPhase(Phase phase)
        {
            if (CurrentPhase == phase) return;
            CurrentPhase = phase;

            var eventId = phase switch
            {
                Phase.Phase1_Dodge => Level3Events.Phase1,
                Phase.Phase2_DualWorld => Level3Events.Phase2,
                Phase.Phase3_Climax => Level3Events.Phase3,
                Phase.LevelComplete => Level3Events.LevelComplete,
                Phase.GameOver => Level3Events.LevelFail,
                _ => null
            };

            if (eventId != null)
                workspace?.RaiseWorkspaceEvent(eventId);
        }
    }
}
