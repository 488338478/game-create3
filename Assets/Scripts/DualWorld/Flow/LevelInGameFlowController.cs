using System.Collections.Generic;
using UnityEngine;

namespace GameCreate3.DualWorld
{
    public sealed class LevelInGameFlowController : MonoBehaviour
    {
        [SerializeField] private List<BaseSubLevelFlow> subLevels = new List<BaseSubLevelFlow>();

        private DualWorldWorkspace workspace;
        private int currentIndex = -1;

        public BaseSubLevelFlow CurrentSubLevel =>
            currentIndex >= 0 && currentIndex < subLevels.Count ? subLevels[currentIndex] : null;

        public void Bind(DualWorldWorkspace ws)
        {
            workspace = ws;
            foreach (var sub in subLevels)
            {
                if (sub != null)
                {
                    sub.Initialize(ws);
                }
            }
        }

        public void BeginFirstSubLevel()
        {
            currentIndex = -1;
            AdvanceToNext();
        }

        private void AdvanceToNext()
        {
            UnsubscribeCurrent();
            currentIndex++;

            if (currentIndex >= subLevels.Count)
            {
                Debug.Log("[LevelInGameFlow] All sub-levels completed.");
                return;
            }

            var sub = subLevels[currentIndex];
            if (sub == null)
            {
                AdvanceToNext();
                return;
            }

            sub.SubLevelFinished += HandleSubLevelFinished;
            sub.EnterPhase(SubLevelPhase.RealityTaskActive);
            Debug.Log($"[LevelInGameFlow] Entering sub-level '{sub.SubLevelId}'.");
        }

        private void HandleSubLevelFinished()
        {
            Debug.Log($"[LevelInGameFlow] Sub-level '{CurrentSubLevel?.SubLevelId}' finished.");
            AdvanceToNext();
        }

        private void UnsubscribeCurrent()
        {
            var sub = CurrentSubLevel;
            if (sub != null)
            {
                sub.SubLevelFinished -= HandleSubLevelFinished;
            }
        }

        private void OnDestroy()
        {
            UnsubscribeCurrent();
        }
    }
}
