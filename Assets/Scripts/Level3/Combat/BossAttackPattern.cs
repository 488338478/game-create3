using System;
using System.Collections.Generic;
using UnityEngine;

namespace GameCreate3.Level3
{
    [CreateAssetMenu(menuName = "GameCreate3/Level3/BossAttackPattern", fileName = "BossAttackPattern")]
    public sealed class BossAttackPattern : ScriptableObject
    {
        public List<BossAttackWave> waves = new List<BossAttackWave>();
    }

    [Serializable]
    public sealed class BossAttackWave
    {
        [Tooltip("波次开始时间（秒）")]
        public float startTime;

        [Tooltip("投射物生成间隔（秒）")]
        public float spawnInterval = 0.8f;

        [Tooltip("波次持续时间（秒），0 表示无限")]
        public float duration;

        [Tooltip("弹幕飞行速度")]
        public float fallSpeed = 3f;

        [Tooltip("生成时缩放倍率")]
        public float spawnScale = 1f;

        [Tooltip("该波次投射物数量上限")]
        public int maxProjectiles = 50;
    }
}
