using UnityEditor;
using UnityEngine;
using static GameCreate3.WorkspaceEventRouter;

namespace GameCreate3.Editor
{
    [CustomPropertyDrawer(typeof(WorkspaceEventRouter.EventBinding))]
    public sealed class WorkspaceEventBindingDrawer : PropertyDrawer
    {
        private static readonly string[] EventTypeLabels =
        {
            "Workspace Completed",
            "Goal",
            "Exit",
            "Pickup",
            "Push (solved)",
            "Dialogue",
            "Custom",
            "Interact",
        };

        // eventType 不需要子 ID 时返回 false
        private static bool NeedsSubId(WorkspaceEventType t) =>
            t != WorkspaceEventType.WorkspaceCompleted;

        // 预览字符串，显示在折叠标题旁
        private static string Preview(SerializedProperty typeProp, SerializedProperty subIdProp)
        {
            var t = (WorkspaceEventType)typeProp.enumValueIndex;
            return t switch
            {
                WorkspaceEventType.WorkspaceCompleted => "workspace.completed",
                WorkspaceEventType.Goal      => $"goal.{subIdProp.stringValue}",
                WorkspaceEventType.Exit      => $"exit.{subIdProp.stringValue}",
                WorkspaceEventType.Pickup    => $"pickup.{subIdProp.stringValue}",
                WorkspaceEventType.Push      => $"push.{subIdProp.stringValue}.solved",
                WorkspaceEventType.Dialogue  => $"dialogue.{subIdProp.stringValue}",
                WorkspaceEventType.Interact  => $"interact.{subIdProp.stringValue}",
                WorkspaceEventType.Custom    => subIdProp.stringValue,
                _                            => subIdProp.stringValue
            };
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            var typeProp  = property.FindPropertyRelative("eventType");
            var subIdProp = property.FindPropertyRelative("subId");
            var eventProp = property.FindPropertyRelative("onEvent");

            var t = (WorkspaceEventType)typeProp.enumValueIndex;
            float h = EditorGUIUtility.singleLineHeight + 2; // eventType 行
            if (NeedsSubId(t))
                h += EditorGUIUtility.singleLineHeight + 2;  // subId 行
            h += EditorGUI.GetPropertyHeight(eventProp) + 2; // UnityEvent
            return h;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var typeProp  = property.FindPropertyRelative("eventType");
            var subIdProp = property.FindPropertyRelative("subId");
            var eventProp = property.FindPropertyRelative("onEvent");

            var t = (WorkspaceEventType)typeProp.enumValueIndex;

            EditorGUI.BeginProperty(position, label, property);

            float lineH = EditorGUIUtility.singleLineHeight;
            float pad   = 2f;
            var   rect  = new Rect(position.x, position.y, position.width, lineH);

            // --- 事件类型下拉 ---
            EditorGUI.LabelField(rect, "Event Type", EditorStyles.boldLabel);
            rect.x     += EditorGUIUtility.labelWidth;
            rect.width -= EditorGUIUtility.labelWidth;
            typeProp.enumValueIndex = EditorGUI.Popup(rect, typeProp.enumValueIndex, EventTypeLabels);
            rect.x     -= EditorGUIUtility.labelWidth;
            rect.width += EditorGUIUtility.labelWidth;
            rect.y     += lineH + pad;

            // --- 子 ID（仅非 WorkspaceCompleted 显示）---
            t = (WorkspaceEventType)typeProp.enumValueIndex;
            if (NeedsSubId(t))
            {
                string subLabel = t switch
                {
                    WorkspaceEventType.Goal     => "Goal Id",
                    WorkspaceEventType.Exit     => "Exit Id",
                    WorkspaceEventType.Pickup   => "Pickup Id",
                    WorkspaceEventType.Push     => "Push Id",
                    WorkspaceEventType.Dialogue => "Dialogue Id",
                    WorkspaceEventType.Interact => "Interact Id",
                    WorkspaceEventType.Custom   => "Event Id",
                    _                           => "Sub Id"
                };
                subIdProp.stringValue = EditorGUI.TextField(
                    new Rect(rect.x, rect.y, rect.width, lineH),
                    subLabel, subIdProp.stringValue);
                rect.y += lineH + pad;
            }

            // --- 预览 ---
            var previewStyle = new GUIStyle(EditorStyles.helpBox)
            {
                fontSize  = 10,
                alignment = TextAnchor.MiddleLeft
            };
            previewStyle.normal.textColor = new Color(0.4f, 0.7f, 1f);
            // 预览显示在 UnityEvent 上方，不占额外高度，直接叠加在 UnityEvent label 行左边小标签
            // 改用右对齐小字提示
            var previewRect = new Rect(rect.x, rect.y, rect.width, lineH);
            EditorGUI.LabelField(previewRect,
                "→  " + Preview(typeProp, subIdProp),
                new GUIStyle(EditorStyles.miniLabel) { normal = { textColor = new Color(0.5f, 0.8f, 0.5f) } });
            rect.y += lineH + pad;

            // --- UnityEvent ---
            var eventRect = new Rect(rect.x, rect.y,
                rect.width, EditorGUI.GetPropertyHeight(eventProp));
            EditorGUI.PropertyField(eventRect, eventProp, new GUIContent("On Event"));

            EditorGUI.EndProperty();
        }
    }
}
