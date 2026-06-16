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
    ///   - 若对象挂有 Rigidbody2D（如单向平台需要物理参与），会自动切到
    ///     FixedUpdate + MovePosition，确保 PlatformEffector2D 正常工作。
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

        [Header("Sprite cycle")]
        [SerializeField, Tooltip("每次回到起点时切换到下一个 sprite，循环。")]
        private Sprite[] sprites;

        private Vector3 basePos;
        private bool initialized;
        private SpriteRenderer spriteRenderer;
        private int spriteIndex;
        private Rigidbody2D rb;
        private bool usePhysicsMovement;

        public Vector3 BasePosition => basePos;
        public int CurrentSpriteIndex => spriteIndex;
        public bool UsePhysicsMovement => usePhysicsMovement;

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

            spriteRenderer = GetComponent<SpriteRenderer>();
            rb = GetComponent<Rigidbody2D>();
            usePhysicsMovement = rb != null;
            spriteIndex = 0;
            if (sprites is { Length: > 0 } && spriteRenderer != null)
            {
                spriteRenderer.sprite = sprites[0];
            }

            initialized = true;
        }

        private void Update()
        {
            if (!initialized) return;
            if (usePhysicsMovement) return; // 走 FixedUpdate
            MoveOneStep(Time.deltaTime);
        }

        private void FixedUpdate()
        {
            if (!initialized) return;
            if (!usePhysicsMovement) return; // 走 Update
            MoveOneStep(Time.fixedDeltaTime);
        }

        public void MoveOneStep(float dt)
        {
            var p = transform.position;
            p.y += speed * dt;
            if (p.y - basePos.y >= loopHeight)
            {
                p.y = basePos.y;
                AdvanceSprite();
            }
            ApplyPosition(p);
        }

        private void AdvanceSprite()
        {
            if (sprites is not { Length: > 0 } || spriteRenderer == null) return;
            spriteIndex = (spriteIndex + 1) % sprites.Length;
            spriteRenderer.sprite = sprites[spriteIndex];
        }

        private void ApplyPosition(Vector3 pos)
        {
            if (rb != null)
                rb.MovePosition(pos);
            else
                transform.position = pos;
        }

        // 暴露给单元测试
        public void SetSpeedForTest(float value) => speed = value;
        public void SetLoopHeightForTest(float value) => loopHeight = value;
        public void SetSpritesForTest(Sprite[] value) => sprites = value;
        public void SetStartPhase01ForTest(float value) => startPhase01 = value;
    }
}
