using UnityEngine;

namespace GameCreate3.DualWorld
{
    /// <summary>
    /// 挂在目标关（L2）场景根：把上一关经 <see cref="DualWorldHandoff"/> 带来的
    /// 「散块位移 + 聊天 log」还原到本场景。
    /// - 散块：必须等 <see cref="AlignmentSubLevelFlow"/> 进入 RealityTaskActive
    ///   （其内部 ResetTask 之后）再还原，否则会被 ResetTask 抹回初始位置。
    ///   故在 Awake 早早订阅 PhaseChanged，等事件来了再套。
    /// - 聊天：把历史前插到当前 log 顶部（L2 开场白之上），保持时间顺序。
    /// consume-once：还原后清空 Pending。
    ///
    /// 执行顺序设得很早：workspace 在 Start 里 Enter → BeginFirstSubLevel → EnterPhase，
    /// 本组件要赶在那之前完成订阅。
    /// </summary>
    [DefaultExecutionOrder(-200)]
    public sealed class DualWorldHandoffRestorer : MonoBehaviour
    {
        private AlignmentSubLevelFlow flow;
        private bool done;

        private void Awake()
        {
            if (DualWorldHandoff.Pending == null) { enabled = false; return; }

            flow = FindObjectOfType<AlignmentSubLevelFlow>(true);
            if (flow != null)
                flow.PhaseChanged += OnPhaseChanged;
            else
                Apply();   // 没有 alignment flow（异常场景）就直接兜底还原一次
        }

        private void OnPhaseChanged(SubLevelPhase phase)
        {
            // RealityTaskActive 是枚举默认值(0)，但 PhaseChanged 只在真正 EnterPhase 时触发，
            // 收到它就代表本次 ResetTask / 开场白都已经跑过了 —— 此刻还原才不会被抹掉。
            if (done || phase != SubLevelPhase.RealityTaskActive) return;
            Apply();
        }

        private void Apply()
        {
            done = true;
            if (flow != null) flow.PhaseChanged -= OnPhaseChanged;

            var snap = DualWorldHandoff.Pending;
            DualWorldHandoff.Pending = null;   // consume-once
            if (snap == null) return;

            // 1) 散块位移（在 ResetTask 之后还原）
            var task = FindObjectOfType<RealityAlignmentTask>(true);
            if (task != null) task.RestoreBlocks(snap);

            // 2) 聊天历史前插到顶部：取出当前 log（拷贝）→ 清空 → 先铺历史 → 再补回原有。
            var chatBox = FindObjectOfType<ChatBoxUI>(true);
            if (chatBox != null && snap.chat != null && snap.chat.Count > 0)
            {
                var existing = chatBox.GetLog();
                chatBox.Clear();
                for (var i = 0; i < snap.chat.Count; i++) chatBox.Append(snap.chat[i]);
                for (var i = 0; i < existing.Count; i++) chatBox.Append(existing[i]);
            }
        }

        private void OnDestroy()
        {
            if (flow != null) flow.PhaseChanged -= OnPhaseChanged;
        }
    }
}
