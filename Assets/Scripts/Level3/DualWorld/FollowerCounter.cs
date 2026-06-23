using System.Collections.Generic;
using UnityEngine;

namespace GameCreate3.Level3
{
    public sealed class FollowerCounter : MonoBehaviour
    {
        [Header("Gain / Loss")]
        [SerializeField] private int passiveGainPerSecond = 10;
        [SerializeField] private int parryGain = 800;
        [SerializeField] private int hitPenalty = 200;

        [Header("Thresholds")]
        [SerializeField] private int[] thresholds = { 2000, 4000, 6000, 8000 };

        [Header("Jump on Complete")]
        [SerializeField] private int sequenceCompleteJump = 100000;

        public int CurrentFollowers { get; private set; }

        private SideScrollWorkspaceBase workspace;
        private bool isActive;
        private float passiveAccumulator;
        private readonly HashSet<int> triggeredThresholds = new HashSet<int>();

        private void Awake()
        {
            workspace = GetComponentInParent<SideScrollWorkspaceBase>(true);
        }

        private void Update()
        {
            if (!isActive) return;

            passiveAccumulator += passiveGainPerSecond * Time.deltaTime;
            if (passiveAccumulator >= 1f)
            {
                var gain = Mathf.FloorToInt(passiveAccumulator);
                passiveAccumulator -= gain;
                AddFollowers(gain);
            }
        }

        // --- WorkspaceEventRouter 调用的 public 入口 ---

        public void OnPhase2()
        {
            isActive = true;
            passiveAccumulator = 0f;
        }

        public void OnParrySuccess()
        {
            AddFollowers(parryGain);
        }

        public void OnPlayerHit()
        {
            SubtractFollowers(hitPenalty);
        }

        public void OnSequenceComplete()
        {
            var oldValue = CurrentFollowers;
            CurrentFollowers = sequenceCompleteJump;
            workspace?.RaiseWorkspaceEvent(Level3Events.FollowerChanged);
            CheckThresholds(oldValue, CurrentFollowers);
        }

        // --- 内部 ---

        private void AddFollowers(int amount)
        {
            var oldValue = CurrentFollowers;
            CurrentFollowers += amount;
            workspace?.RaiseWorkspaceEvent(Level3Events.FollowerChanged);
            CheckThresholds(oldValue, CurrentFollowers);
        }

        private void SubtractFollowers(int amount)
        {
            var oldValue = CurrentFollowers;
            CurrentFollowers = Mathf.Max(0, CurrentFollowers - amount);
            workspace?.RaiseWorkspaceEvent(Level3Events.FollowerChanged);
            CheckThresholds(oldValue, CurrentFollowers);
        }

        private void CheckThresholds(int oldValue, int newValue)
        {
            foreach (var threshold in thresholds)
            {
                if (!triggeredThresholds.Contains(threshold) && newValue >= threshold)
                {
                    triggeredThresholds.Add(threshold);
                    workspace?.RaiseWorkspaceEvent(Level3Events.Follower(threshold));
                }
            }
        }
    }
}
