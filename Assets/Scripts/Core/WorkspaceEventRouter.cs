using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace GameCreate3
{
    /// <summary>
    /// 把 <see cref="SideScrollWorkspaceBase.WorkspaceEventRaised"/> 的字符串事件
    /// 转成可在 Inspector 配置的 UnityEvent，用法与 Button.onClick 完全一致。
    ///
    /// 挂在 Workspace 根节点，Awake 时自动订阅同 GameObject 上的 Workspace。
    /// 也可手动赋值 <see cref="workspace"/> 字段指向任意 Workspace。
    /// </summary>
    public sealed class WorkspaceEventRouter : MonoBehaviour
    {
        public enum WorkspaceEventType
        {
            WorkspaceCompleted,  // workspace.completed
            Goal,                // goal.<subId>
            Exit,                // exit.<subId>
            Pickup,              // pickup.<subId>
            Push,                // push.<subId>.solved
            Dialogue,            // dialogue.<subId>
            Custom,              // 自由填写
            // 新增枚举值必须追加在末尾，否则会让旧数据按 enumValueIndex 错位。
            Interact,            // interact.<subId>
        }

        [Serializable]
        public sealed class EventBinding
        {
            public WorkspaceEventType eventType = WorkspaceEventType.WorkspaceCompleted;

            [Tooltip("事件关联的子 ID，例如 goal 对应的 goalId、exit 对应的 exitId 等。WorkspaceCompleted 不需要填。")]
            public string subId;

            [Tooltip("事件触发时调用，可拖入任意组件的任意方法（包括 SceneRouterProxy.Go）")]
            public UnityEvent onEvent;

            public string ResolvedEventId => eventType switch
            {
                WorkspaceEventType.WorkspaceCompleted => "workspace.completed",
                WorkspaceEventType.Goal               => $"goal.{subId}",
                WorkspaceEventType.Exit               => $"exit.{subId}",
                WorkspaceEventType.Pickup             => $"pickup.{subId}",
                WorkspaceEventType.Push               => $"push.{subId}.solved",
                WorkspaceEventType.Dialogue           => $"dialogue.{subId}",
                WorkspaceEventType.Interact           => $"interact.{subId}",
                WorkspaceEventType.Custom             => subId,
                _                                     => subId
            };
        }

        [Tooltip("留空则自动在同 GameObject 上查找 SideScrollWorkspaceBase")]
        [SerializeField] private SideScrollWorkspaceBase workspace;

        [SerializeField] private List<EventBinding> bindings = new List<EventBinding>();

        private void Awake()
        {
            if (workspace == null)
                workspace = GetComponentInParent<SideScrollWorkspaceBase>(true);

            if (workspace == null)
            {
                Debug.LogWarning("[WorkspaceEventRouter] 找不到 SideScrollWorkspaceBase，请手动赋值或放在 Workspace 节点上。", this);
                return;
            }

            workspace.WorkspaceEventRaised += OnWorkspaceEvent;
        }

        private void OnDestroy()
        {
            if (workspace != null)
                workspace.WorkspaceEventRaised -= OnWorkspaceEvent;
        }

        private void OnWorkspaceEvent(string eventId)
        {
            foreach (var binding in bindings)
            {
                if (binding.ResolvedEventId == eventId)
                    binding.onEvent?.Invoke();
            }
        }
    }
}
