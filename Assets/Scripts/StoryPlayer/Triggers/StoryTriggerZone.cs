using UnityEngine;

namespace GameCreate3.StoryPlayer
{
    /// <summary>
    /// 玩家走进 Collider 区域时播放剧情。一次性 / 可重复可配。
    /// 用法：拖 prefab → 设大小 / 位置 → 把 sequence asset 拖到字段。
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public sealed class StoryTriggerZone : MonoBehaviour
    {
        [SerializeField] private StorySequence sequence;
        [SerializeField] private bool oneShot = true;
        [SerializeField] private LayerMask targetLayers = ~0;

        private bool fired;

        private void Reset()
        {
            // 自动把 Collider 设为 trigger，免得新建 prefab 时漏配
            if (TryGetComponent<Collider2D>(out var c)) c.isTrigger = true;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (oneShot && fired) return;
            if (sequence == null) return;
            if (((1 << other.gameObject.layer) & targetLayers.value) == 0) return;

            fired = true;
            StoryPlayerService.Play(sequence);
        }
    }
}
