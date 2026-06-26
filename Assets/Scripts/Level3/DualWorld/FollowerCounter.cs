using System;
using System.Collections.Generic;
using UnityEngine;

namespace GameCreate3.Level3
{
    public sealed class FollowerCounter : MonoBehaviour
    {
        [Header("Gain / Loss")]
        [SerializeField] private int initialFollowers = 1000;
        [SerializeField] private int passiveGainPerSecond = 10;
        [SerializeField] private int parryGain = 800;
        [SerializeField] private int hitPenalty = 200;
        [SerializeField] private int hitPenaltyMultiplier = 2;

        [Header("Thresholds")]
        [SerializeField] private int[] thresholds = { 2000, 4000, 6000, 8000 };

        [Header("Jump on Complete")]
        [SerializeField] private int sequenceCompleteJump = 100000;

        public int CurrentFollowers { get; private set; }

        /// <summary>参数为粉丝变动量（正=涨粉，负=掉粉），仅离散事件触发。</summary>
        public event Action<int> FollowerDeltaOccurred;

        private SideScrollWorkspaceBase workspace;
        private bool isActive;
        private float passiveAccumulator;
        private readonly HashSet<int> triggeredThresholds = new HashSet<int>();

        private void Awake()
        {
            workspace = GetComponentInParent<SideScrollWorkspaceBase>(true);
            CurrentFollowers = initialFollowers;
        }

        private void Update()
        {
            if (!isActive) return;

            passiveAccumulator += passiveGainPerSecond * Time.deltaTime;
            if (passiveAccumulator >= 1f)
            {
                var gain = Mathf.FloorToInt(passiveAccumulator);
                passiveAccumulator -= gain;
                AddFollowers(gain, fireDelta: false);
            }
        }

        // --- WorkspaceEventRouter 调用的 public 入口 ---

        public void OnPhase2()
        {
            isActive = true;
            CurrentFollowers = initialFollowers;
            passiveAccumulator = 0f;
            workspace?.RaiseWorkspaceEvent(Level3Events.FollowerChanged);
        }

        public void OnParrySuccess()
        {
            if (!isActive) return;
            AddFollowers(parryGain, fireDelta: true);
        }

        public void OnPlayerHit()
        {
            if (!isActive) return;
            SubtractFollowers(hitPenalty * hitPenaltyMultiplier);
        }

        public void OnSequenceComplete()
        {
            var oldValue = CurrentFollowers;
            CurrentFollowers = sequenceCompleteJump;
            workspace?.RaiseWorkspaceEvent(Level3Events.FollowerChanged);
            CheckThresholds(oldValue, CurrentFollowers);
            FollowerDeltaOccurred?.Invoke(CurrentFollowers - oldValue);
        }

        // --- 内部 ---

        private void AddFollowers(int amount, bool fireDelta)
        {
            var oldValue = CurrentFollowers;
            CurrentFollowers += amount;
            workspace?.RaiseWorkspaceEvent(Level3Events.FollowerChanged);
            CheckThresholds(oldValue, CurrentFollowers);
            if (fireDelta)
                FollowerDeltaOccurred?.Invoke(amount);
        }

        private void SubtractFollowers(int amount)
        {
            var oldValue = CurrentFollowers;
            CurrentFollowers = Mathf.Max(0, CurrentFollowers - amount);
            workspace?.RaiseWorkspaceEvent(Level3Events.FollowerChanged);
            CheckThresholds(oldValue, CurrentFollowers);
            FollowerDeltaOccurred?.Invoke(-(oldValue - CurrentFollowers));
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
