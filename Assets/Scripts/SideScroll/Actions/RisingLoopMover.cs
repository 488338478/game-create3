using UnityEngine;

namespace GameCreate3
{
    /// <summary>
    /// 自管理位置：每帧向上移动，超过 loopHeight 后立即归位继续上升，形成无限循环。
    /// 适合烟雾 / 泡泡 / 灰尘 / 粒子等持续上飘效果，挂在被 Spawn 出来的对象上即可。
    ///
    /// 设计要点：
    ///   - 初始位置在 OnEnable 时缓存，回归点就是被 Spawn 出来的那一刻的世界坐标。
    ///   - 若开启 randomize，会在 OnEnable 时为每个实例独立摇 speed/loopHeight，
    ///     避免一堆同源 Spawn 的对象做完全一致的运动（视觉上一坨）。
    ///   - startPhase01 用来把若干同时生成的实例错开高度，看起来已经在飘。
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class RisingLoopMover : MonoBehaviour
    {
        [SerializeField, Tooltip("向上速度，单位/秒。randomize=true 时被 speedRange 覆盖。")]
        private float speed = 1.2f;

        [SerializeField, Tooltip("相对初始 Y 的循环高度；达到该高度后回到初始位置。randomize=true 时被 loopHeightRange 覆盖。")]
        private float loopHeight = 3.0f;

        [SerializeField, Range(0f, 1f), Tooltip("起始抬高比例 (0~1)，只整体抬高本实例的循环区间，不压缩 loopHeight；randomize=true 时被随机化。")]
        private float startPhase01 = 0f;

        [SerializeField, Tooltip("是否随机化 speed / loopHeight / startPhase01。")]
        private bool randomize = true;

        [SerializeField] private Vector2 speedRange = new Vector2(0.8f, 1.6f);
        [SerializeField] private Vector2 loopHeightRange = new Vector2(2.0f, 4.0f);

        private Vector3 basePos;
        private bool initialized;

        private void OnEnable()
        {
            if (randomize)
            {
                speed = Random.Range(speedRange.x, speedRange.y);
                loopHeight = Random.Range(loopHeightRange.x, loopHeightRange.y);
                startPhase01 = Random.value;
            }

            if (loopHeight <= 0f) loopHeight = 0.0001f;

            // 初始抬高只改变本实例的循环底点，不改变 loopHeight 本身。
            basePos = transform.position;
            basePos.y += loopHeight * Mathf.Clamp01(startPhase01);
            transform.position = basePos;

            initialized = true;
        }

        private void Update()
        {
            if (!initialized) return;

            var p = transform.position;
            p.y += speed * Time.deltaTime;
            if (p.y - basePos.y >= loopHeight)
            {
                p.y = basePos.y;
            }
            transform.position = p;
        }
    }
}
