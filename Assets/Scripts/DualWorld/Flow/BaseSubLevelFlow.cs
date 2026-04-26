using System;
using UnityEngine;

namespace GameCreate3.DualWorld
{
    public abstract class BaseSubLevelFlow : MonoBehaviour
    {
        [SerializeField] private string subLevelId = "sublevel";

        public string SubLevelId => subLevelId;
        public SubLevelPhase CurrentPhase { get; private set; }
        public DualWorldWorkspace Workspace { get; private set; }

        public event Action<SubLevelPhase> PhaseChanged;
        public event Action SubLevelFinished;

        public virtual void Initialize(DualWorldWorkspace workspace)
        {
            Workspace = workspace;
            OnInitialized();
        }

        public void EnterPhase(SubLevelPhase phase)
        {
            CurrentPhase = phase;
            OnPhaseEntered(phase);
            PhaseChanged?.Invoke(phase);

            if (phase == SubLevelPhase.SubLevelCompleted)
            {
                SubLevelFinished?.Invoke();
            }
        }

        public abstract void OnRealitySubmit(RealitySubmitResult result);
        public abstract void OnDreamComplete();
        public abstract void OnTraversalReachedExit();

        protected virtual void OnInitialized() { }
        protected virtual void OnPhaseEntered(SubLevelPhase phase) { }
    }
}
