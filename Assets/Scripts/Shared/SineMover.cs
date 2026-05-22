using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GameCreate3
{
    /// <summary>
    /// 被触发时将一组物件沿正弦波动轨迹移动到目标点。
    /// 轨迹 = 主方向线性插值 + 垂直方向正弦波偏移。
    /// </summary>
    public sealed class SineMover : MonoBehaviour
    {
        public enum Mode
        {
            Once,       // 单程
            PingPong,   // 去了再回
            Loop,       // 持续来回
        }

        [Header("移动目标")]
        [Tooltip("目标位置。留空则用 targetPosition 世界坐标。")]
        [SerializeField] private Transform target;
        [SerializeField] private Vector3 targetPosition;

        [Header("被移动的物件")]
        [Tooltip("所有需要同步移动的物件。留空则只移动自身。")]
        [SerializeField] private List<Transform> subjects = new List<Transform>();

        [Header("参数")]
        [SerializeField] private float duration  = 1f;
        [Tooltip("正弦波振幅（垂直于运动方向的偏移距离）")]
        [SerializeField] private float amplitude = 0.5f;
        [Tooltip("正弦波周期数（1 = 一个完整波形）")]
        [SerializeField] private float cycles    = 1f;
        [SerializeField] private Mode  mode      = Mode.Once;

        private List<Vector3> originPositions = new List<Vector3>();
        private Coroutine moveCoroutine;

        private void Awake()
        {
            if (subjects.Count == 0)
                subjects.Add(transform);

            foreach (var s in subjects)
                originPositions.Add(s != null ? s.position : Vector3.zero);
        }

        /// <summary>开始移动，可挂在 UnityEvent 上。</summary>
        public void Trigger()
        {
            if (moveCoroutine != null) StopCoroutine(moveCoroutine);
            moveCoroutine = mode switch
            {
                Mode.Once     => StartCoroutine(MoveOnce()),
                Mode.PingPong => StartCoroutine(MovePingPong()),
                Mode.Loop     => StartCoroutine(MoveLoop()),
                _             => StartCoroutine(MoveOnce())
            };
        }

        /// <summary>停止并复位。</summary>
        public void ResetMove()
        {
            if (moveCoroutine != null) { StopCoroutine(moveCoroutine); moveCoroutine = null; }
            for (int i = 0; i < subjects.Count; i++)
                if (subjects[i] != null) subjects[i].position = originPositions[i];
        }

        // ── 轨迹核心 ────────────────────────────────────────────────

        private Vector3 TargetWorldPosition =>
            target != null ? target.position : targetPosition;

        /// <summary>
        /// 计算 t ∈ [0,1] 时的世界坐标偏移（相对于起点）。
        /// 主轴线性前进，垂直轴正弦波动。
        /// </summary>
        private static Vector3 SineOffset(Vector3 from, Vector3 to, float t, float amp, float cyc)
        {
            var dir  = to - from;
            var main = dir * t;

            // 2D 垂直方向（Z 轴不动）
            var perp = new Vector3(-dir.normalized.y, dir.normalized.x, 0f);
            var wave = perp * (amp * Mathf.Sin(t * cyc * Mathf.PI * 2f));

            return main + wave;
        }

        private IEnumerator MoveTo(List<Vector3> froms, Vector3 toOffset)
        {
            // toOffset 是相对 froms[0] 的目标，其余物件同步偏移
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                for (int i = 0; i < subjects.Count; i++)
                {
                    if (subjects[i] == null) continue;
                    subjects[i].position = froms[i] + SineOffset(froms[i], froms[i] + toOffset, t, amplitude, cycles);
                }
                yield return null;
            }
            // 落点精确归位（消除最后一帧误差）
            for (int i = 0; i < subjects.Count; i++)
                if (subjects[i] != null) subjects[i].position = froms[i] + toOffset;
        }

        // ── 模式协程 ────────────────────────────────────────────────

        private IEnumerator MoveOnce()
        {
            var dest   = TargetWorldPosition;
            var froms  = CurrentPositions();
            var offset = dest - froms[0];          // 以第一个物件为基准算偏移
            yield return MoveTo(froms, offset);
            moveCoroutine = null;
        }

        private IEnumerator MovePingPong()
        {
            var dest   = TargetWorldPosition;
            var froms  = CurrentPositions();
            var offset = dest - froms[0];
            yield return MoveTo(froms, offset);

            var returnFroms  = CurrentPositions();
            var returnOffset = froms[0] - returnFroms[0];
            yield return MoveTo(returnFroms, returnOffset);
            moveCoroutine = null;
        }

        private IEnumerator MoveLoop()
        {
            while (true)
            {
                var dest   = TargetWorldPosition;
                var froms  = CurrentPositions();
                var offset = dest - froms[0];
                yield return MoveTo(froms, offset);

                var returnFroms  = CurrentPositions();
                var returnOffset = froms[0] - returnFroms[0];
                yield return MoveTo(returnFroms, returnOffset);
            }
        }

        private List<Vector3> CurrentPositions()
        {
            var list = new List<Vector3>();
            foreach (var s in subjects)
                list.Add(s != null ? s.position : Vector3.zero);
            return list;
        }
    }
}
