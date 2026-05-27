using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

namespace GameCreate3
{
    /// <summary>
    /// 按配置表一次性生成一组 prefab 实例（典型用法：泡泡 / 灰尘 / 光点）。
    /// 通过 UnityEvent 触发 Spawn()。
    /// 被 Spawn 出来的对象自己负责持续行为（如挂 RisingLoopMover 做循环上升）。
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class PrefabBurstSpawner : MonoBehaviour
    {
        [Serializable]
        public struct SpawnEntry
        {
            [Tooltip("该位置要生成的 prefab。")]
            public GameObject prefab;

            [FormerlySerializedAs("positionOffset")]
            [FormerlySerializedAs("worldPosition")]
            [Tooltip("相对生成锚点的本地偏移。生成锚点优先级：parentOverride > spawnParent > Spawner 自身；无偏移时就在锚点的世界位置。")]
            public Vector3 localOffset;

            [Tooltip("该实例额外旋转。")]
            public Vector3 eulerOffset;

            [Tooltip("该实例的生成锚点和父节点；留空则使用默认 spawnParent。")]
            public Transform parentOverride;

            [Tooltip("是否继承生成锚点的世界旋转。")]
            public bool inheritSpawnerRotation;
        }

        [SerializeField, Tooltip("逐项配置要生成的 prefab 与位置。")]
        private SpawnEntry[] entries = Array.Empty<SpawnEntry>();

        [SerializeField, Tooltip("默认生成锚点和父节点；entry.parentOverride 留空时使用。再留空 = Spawner 自身。")]
        private Transform spawnParent;

        [SerializeField, Tooltip("勾上则每次 Spawn 前清掉上一次由本组件生成的实例。")]
        private bool clearPreviousBeforeSpawn = false;

        private readonly List<GameObject> spawnedInstances = new List<GameObject>();

        public void Spawn()
        {
            if (entries == null || entries.Length == 0)
            {
                Debug.LogWarning($"[PrefabBurstSpawner] '{name}' entries 未配置。", this);
                return;
            }

            if (clearPreviousBeforeSpawn)
            {
                ClearSpawnedInstances();
            }

            for (int i = 0; i < entries.Length; i++)
            {
                var entry = entries[i];
                if (entry.prefab == null)
                {
                    Debug.LogWarning($"[PrefabBurstSpawner] '{name}' entries[{i}] prefab 未配置，已跳过。", this);
                    continue;
                }

                var spawnTransform = GetSpawnTransform(entry);
                var baseRotation = entry.inheritSpawnerRotation ? spawnTransform.rotation : Quaternion.identity;
                var rotation = baseRotation * Quaternion.Euler(entry.eulerOffset);
                var position = spawnTransform.TransformPoint(entry.localOffset);

                var instance = Instantiate(entry.prefab, position, rotation, spawnTransform);
                instance.transform.position = position;
                spawnedInstances.Add(instance);
            }
        }

        public void ClearSpawnedInstances()
        {
            for (int i = spawnedInstances.Count - 1; i >= 0; i--)
            {
                var instance = spawnedInstances[i];
                if (instance != null) Destroy(instance);
            }
            spawnedInstances.Clear();
        }

        private Transform GetSpawnTransform(SpawnEntry entry)
        {
            if (entry.parentOverride != null) return entry.parentOverride;
            if (spawnParent != null) return spawnParent;
            return transform;
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(0.4f, 1f, 0.6f, 0.5f);
            if (entries == null) return;

            for (int i = 0; i < entries.Length; i++)
            {
                var spawnTransform = GetSpawnTransform(entries[i]);
                var p = spawnTransform.TransformPoint(entries[i].localOffset);
                Gizmos.DrawLine(spawnTransform.position, p);
                Gizmos.DrawWireSphere(p, 0.08f);
            }
        }
#endif
    }
}
