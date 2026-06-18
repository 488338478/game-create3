using System.Collections.Generic;
using UnityEngine;

namespace GameCreate3.DualWorld
{
    /// <summary>
    /// 跨场景把 reality 散块位移 + 聊天 log 从一关带到下一关（L1.1 → L2）的静态载体。
    /// 只带「与本关绝对布局无关」的量：散块带相对各自初始位置的位移（用 dragBounds 尺寸做归一参考），
    /// 聊天带整条 log 数据。consume-once：目标关的 DualWorldHandoffRestorer 还原后置空。
    /// </summary>
    public static class DualWorldHandoff
    {
        public sealed class Snapshot
        {
            /// 采集侧 dragBounds 的尺寸；还原时与目标关 dragBounds 尺寸做等比换算。无 bounds 时为 zero。
            public Vector2 captureBoundsSize;

            /// 按 block 顺序的锚定位移（anchoredPosition − 各自 initialPosition）。两关 block 一一对应。
            public List<Vector2> blockDeltas = new List<Vector2>();

            /// 聊天 log 全量（ChatLogEntry 为纯托管对象，跨场景卸载仍存活）。
            public List<ChatLogEntry> chat = new List<ChatLogEntry>();
        }

        /// 待还原的快照；目标关 DualWorldHandoffRestorer 消费后置空。
        public static Snapshot Pending;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatic()
        {
            // Disable Domain Reload 模式下 static 会跨 play 残留，运行入口清一遍。
            Pending = null;
        }
    }
}
