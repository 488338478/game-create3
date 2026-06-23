using System;
using System.Collections.Generic;
using UnityEngine;

namespace GameCreate3.Level3
{
    /// <summary>
    /// Boss 攻击模式数据资产。定义波次、生成间隔、投射物类型等。
    /// </summary>
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

        [Tooltip("投射物预制体列表（随机选取）")]
        public List<GameObject> projectilePrefabs = new List<GameObject>();

        [Tooltip("生成类型")]
        public SpawnType spawnType = SpawnType.RandomX;

        [Tooltip("下落速度")]
        public float fallSpeed = 3f;

        [Tooltip("水平摆动幅度（0=直线下落）")]
        public float swayAmplitude;

        [Tooltip("水平摆动频率")]
        public float swayFrequency = 1f;

        [Tooltip("该波次投射物数量上限")]
        public int maxProjectiles = 50;
    }

    public enum SpawnType
    {
        RandomX,   // 屏幕顶部随机 X
        Targeted,  // 瞄准玩家位置
        Spread,    // 扇形散布
    }
}
