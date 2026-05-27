using GameCreate3.StoryPlayer;
using UnityEditor;
using UnityEngine;

namespace GameCreate3.Editor
{
    [CustomEditor(typeof(StorySequence))]
    public sealed class StorySequenceTextLayoutEditor : UnityEditor.Editor
    {
        private const float CanvasWidth = 1920f;
        private const float CanvasHeight = 1080f;
        private const float HandleSize = 10f;

        private int selectedPageIndex;
        private int selectedTextIndex;
        private DragMode dragMode = DragMode.None;
        private Vector2 dragStartMouse;
        private Rect dragStartRect;

        private enum DragMode
        {
            None,
            Move,
            Left,
            Right,
            Top,
            Bottom,
            TopLeft,
            TopRight,
            BottomLeft,
            BottomRight
        }

        private void OnEnable()
        {
            SceneView.duringSceneGui += DrawScenePreview;
        }

        private void OnDisable()
        {
            SceneView.duringSceneGui -= DrawScenePreview;
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            DrawDefaultInspector();
            DrawLayoutToolInspector();
            serializedObject.ApplyModifiedProperties();
        }

        private void DrawLayoutToolInspector()
        {
            var pagesProp = serializedObject.FindProperty("pages");
            if (pagesProp == null || pagesProp.arraySize == 0)
            {
                return;
            }

            EditorGUILayout.Space(12f);
            EditorGUILayout.LabelField("Scene Text Layout Tool", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Pick a page and text block, then drag the rectangle in Scene view. Drag the center to move, or drag edges/corners to resize.", MessageType.Info);

            selectedPageIndex = Mathf.Clamp(selectedPageIndex, 0, pagesProp.arraySize - 1);
            selectedPageIndex = EditorGUILayout.IntSlider("Page", selectedPageIndex, 0, pagesProp.arraySize - 1);

            var pageProp = pagesProp.GetArrayElementAtIndex(selectedPageIndex);
            var textBlocksProp = pageProp.FindPropertyRelative("textBlocks");
            if (textBlocksProp == null || textBlocksProp.arraySize == 0)
            {
                EditorGUILayout.HelpBox("This page has no text blocks.", MessageType.Warning);
                return;
            }

            selectedTextIndex = Mathf.Clamp(selectedTextIndex, 0, textBlocksProp.arraySize - 1);
            selectedTextIndex = EditorGUILayout.IntSlider("Text Block", selectedTextIndex, 0, textBlocksProp.arraySize - 1);

            var textBlockProp = textBlocksProp.GetArrayElementAtIndex(selectedTextIndex);
            EditorGUILayout.LabelField("Content", GetPreviewText(textBlockProp), EditorStyles.wordWrappedMiniLabel);

            if (GUILayout.Button("Enable Layout Override / Initialize Default Box"))
            {
                Undo.RecordObject(target, "Enable story text layout override");
                InitializeDefaultLayout(textBlockProp);
                serializedObject.ApplyModifiedProperties();
                EditorUtility.SetDirty(target);
                SceneView.RepaintAll();
            }

            if (GUILayout.Button("Frame Scene Preview"))
            {
                SceneView.RepaintAll();
            }
        }

        private void DrawScenePreview(SceneView sceneView)
        {
            if (target == null)
            {
                return;
            }

            serializedObject.Update();

            var pagesProp = serializedObject.FindProperty("pages");
            if (pagesProp == null || pagesProp.arraySize == 0)
            {
                return;
            }

            selectedPageIndex = Mathf.Clamp(selectedPageIndex, 0, pagesProp.arraySize - 1);
            var pageProp = pagesProp.GetArrayElementAtIndex(selectedPageIndex);
            var textBlocksProp = pageProp.FindPropertyRelative("textBlocks");
            if (textBlocksProp == null || textBlocksProp.arraySize == 0)
            {
                return;
            }

            selectedTextIndex = Mathf.Clamp(selectedTextIndex, 0, textBlocksProp.arraySize - 1);
            var textBlockProp = textBlocksProp.GetArrayElementAtIndex(selectedTextIndex);

            Handles.BeginGUI();

            var previewRect = GetPreviewRect(sceneView.position);
            DrawCanvasPreview(previewRect, pageProp);

            var textRect = LayoutToGuiRect(previewRect, textBlockProp);
            DrawTextBox(previewRect, textRect, textBlockProp);
            HandleDrag(previewRect, textRect, textBlockProp);

            Handles.EndGUI();
            serializedObject.ApplyModifiedProperties();
        }

        private static Rect GetPreviewRect(Rect sceneViewRect)
        {
            const float margin = 24f;
            var availableWidth = sceneViewRect.width - margin * 2f;
            var availableHeight = sceneViewRect.height - margin * 2f;
            var width = availableWidth;
            var height = width * CanvasHeight / CanvasWidth;

            if (height > availableHeight)
            {
                height = availableHeight;
                width = height * CanvasWidth / CanvasHeight;
            }

            return new Rect(
                (sceneViewRect.width - width) * 0.5f,
                (sceneViewRect.height - height) * 0.5f,
                width,
                height);
        }

        private static void DrawCanvasPreview(Rect previewRect, SerializedProperty pageProp)
        {
            EditorGUI.DrawRect(previewRect, Color.black);

            var backgroundProp = pageProp.FindPropertyRelative("backgroundImage");
            var sprite = backgroundProp?.objectReferenceValue as Sprite;
            if (sprite != null && sprite.texture != null)
            {
                var texture = sprite.texture;
                var textureRect = sprite.textureRect;
                var uv = new Rect(
                    textureRect.x / texture.width,
                    textureRect.y / texture.height,
                    textureRect.width / texture.width,
                    textureRect.height / texture.height);
                GUI.DrawTextureWithTexCoords(previewRect, texture, uv, true);
            }

            GUI.Box(previewRect, GUIContent.none);
        }

        private static void DrawTextBox(Rect previewRect, Rect textRect, SerializedProperty textBlockProp)
        {
            var fill = new Color(0.1f, 0.45f, 1f, 0.18f);
            var border = new Color(0.3f, 0.75f, 1f, 1f);
            EditorGUI.DrawRect(textRect, fill);
            DrawBorder(textRect, border, 2f);

            var content = GetPreviewText(textBlockProp);
            var labelRect = new Rect(textRect.x + 8f, textRect.y + 8f, Mathf.Max(20f, textRect.width - 16f), Mathf.Max(20f, textRect.height - 16f));
            GUI.Label(labelRect, content, EditorStyles.whiteLabel);

            DrawHandle(GetHandleRect(textRect, DragMode.TopLeft));
            DrawHandle(GetHandleRect(textRect, DragMode.Top));
            DrawHandle(GetHandleRect(textRect, DragMode.TopRight));
            DrawHandle(GetHandleRect(textRect, DragMode.Left));
            DrawHandle(GetHandleRect(textRect, DragMode.Right));
            DrawHandle(GetHandleRect(textRect, DragMode.BottomLeft));
            DrawHandle(GetHandleRect(textRect, DragMode.Bottom));
            DrawHandle(GetHandleRect(textRect, DragMode.BottomRight));

            var title = $"Page/Text: drag to edit ({Mathf.RoundToInt(previewRect.width)}x{Mathf.RoundToInt(previewRect.height)} preview)";
            GUI.Label(new Rect(previewRect.x, previewRect.y - 20f, previewRect.width, 18f), title, EditorStyles.miniBoldLabel);
        }

        private void HandleDrag(Rect previewRect, Rect textRect, SerializedProperty textBlockProp)
        {
            var evt = Event.current;
            var controlId = GUIUtility.GetControlID(FocusType.Passive);

            AddResizeCursors(textRect);
            EditorGUIUtility.AddCursorRect(textRect, MouseCursor.MoveArrow);

            if (evt.type == EventType.MouseDown && evt.button == 0)
            {
                var mode = HitTest(textRect, evt.mousePosition);
                if (mode != DragMode.None)
                {
                    GUIUtility.hotControl = controlId;
                    dragMode = mode;
                    dragStartMouse = evt.mousePosition;
                    dragStartRect = textRect;
                    evt.Use();
                }
            }

            if (GUIUtility.hotControl == controlId && evt.type == EventType.MouseDrag)
            {
                var nextRect = ApplyDrag(dragStartRect, evt.mousePosition - dragStartMouse, dragMode);
                nextRect = ClampToPreview(nextRect, previewRect);

                Undo.RecordObject(target, "Drag story text layout");
                WriteGuiRectToLayout(previewRect, nextRect, textBlockProp);
                serializedObject.ApplyModifiedProperties();
                EditorUtility.SetDirty(target);
                Repaint();
                SceneView.RepaintAll();
                evt.Use();
            }

            if (GUIUtility.hotControl == controlId && evt.type == EventType.MouseUp)
            {
                GUIUtility.hotControl = 0;
                dragMode = DragMode.None;
                evt.Use();
            }
        }

        private static Rect LayoutToGuiRect(Rect previewRect, SerializedProperty textBlockProp)
        {
            var overrideProp = textBlockProp.FindPropertyRelative("overrideTextContainer");
            if (overrideProp != null && !overrideProp.boolValue)
            {
                return DefaultTextRect(previewRect);
            }

            var anchorMin = ReadVector2(textBlockProp, "textAnchorMin", Vector2.zero);
            var anchorMax = ReadVector2(textBlockProp, "textAnchorMax", Vector2.one);
            var pivot = ReadVector2(textBlockProp, "textPivot", new Vector2(0.5f, 0.5f));
            var anchoredPosition = ReadVector2(textBlockProp, "textAnchoredPosition", Vector2.zero);
            var sizeDelta = ReadVector2(textBlockProp, "textSizeDelta", Vector2.zero);

            var parentSize = new Vector2(CanvasWidth, CanvasHeight);
            var anchorRectMin = Vector2.Scale(anchorMin, parentSize);
            var anchorRectMax = Vector2.Scale(anchorMax, parentSize);
            var anchorRectSize = anchorRectMax - anchorRectMin;
            var size = anchorRectSize + sizeDelta;
            size.x = Mathf.Max(1f, size.x);
            size.y = Mathf.Max(1f, size.y);

            var pivotPosition = anchorRectMin + Vector2.Scale(anchorRectSize, pivot) + anchoredPosition;
            var canvasMin = pivotPosition - Vector2.Scale(size, pivot);
            var canvasMax = canvasMin + size;

            return CanvasRectToGuiRect(previewRect, canvasMin, canvasMax);
        }

        private static void WriteGuiRectToLayout(Rect previewRect, Rect guiRect, SerializedProperty textBlockProp)
        {
            textBlockProp.FindPropertyRelative("overrideTextContainer").boolValue = true;

            var anchorMin = ReadVector2(textBlockProp, "textAnchorMin", Vector2.zero);
            var anchorMax = ReadVector2(textBlockProp, "textAnchorMax", Vector2.one);
            var pivot = ReadVector2(textBlockProp, "textPivot", new Vector2(0.5f, 0.5f));

            var canvasMin = new Vector2(
                (guiRect.xMin - previewRect.xMin) / previewRect.width * CanvasWidth,
                CanvasHeight - (guiRect.yMax - previewRect.yMin) / previewRect.height * CanvasHeight);
            var canvasMax = new Vector2(
                (guiRect.xMax - previewRect.xMin) / previewRect.width * CanvasWidth,
                CanvasHeight - (guiRect.yMin - previewRect.yMin) / previewRect.height * CanvasHeight);

            var parentSize = new Vector2(CanvasWidth, CanvasHeight);
            var anchorRectMin = Vector2.Scale(anchorMin, parentSize);
            var anchorRectMax = Vector2.Scale(anchorMax, parentSize);
            var anchorRectSize = anchorRectMax - anchorRectMin;
            var size = canvasMax - canvasMin;
            var pivotPosition = canvasMin + Vector2.Scale(size, pivot);
            var anchoredPosition = pivotPosition - anchorRectMin - Vector2.Scale(anchorRectSize, pivot);

            textBlockProp.FindPropertyRelative("textAnchoredPosition").vector2Value = anchoredPosition;
            textBlockProp.FindPropertyRelative("textSizeDelta").vector2Value = size - anchorRectSize;
        }

        private static void InitializeDefaultLayout(SerializedProperty textBlockProp)
        {
            textBlockProp.FindPropertyRelative("overrideTextContainer").boolValue = true;
            textBlockProp.FindPropertyRelative("textAnchorMin").vector2Value = new Vector2(0f, 0f);
            textBlockProp.FindPropertyRelative("textAnchorMax").vector2Value = new Vector2(1f, 0.3f);
            textBlockProp.FindPropertyRelative("textPivot").vector2Value = new Vector2(0.5f, 0.5f);
            textBlockProp.FindPropertyRelative("textAnchoredPosition").vector2Value = Vector2.zero;
            textBlockProp.FindPropertyRelative("textSizeDelta").vector2Value = new Vector2(-100f, -40f);
        }

        private static Rect DefaultTextRect(Rect previewRect)
        {
            var min = new Vector2(50f, 20f);
            var max = new Vector2(CanvasWidth - 50f, CanvasHeight * 0.3f - 20f);
            return CanvasRectToGuiRect(previewRect, min, max);
        }

        private static Rect CanvasRectToGuiRect(Rect previewRect, Vector2 canvasMin, Vector2 canvasMax)
        {
            var x = previewRect.xMin + canvasMin.x / CanvasWidth * previewRect.width;
            var y = previewRect.yMin + (CanvasHeight - canvasMax.y) / CanvasHeight * previewRect.height;
            var width = (canvasMax.x - canvasMin.x) / CanvasWidth * previewRect.width;
            var height = (canvasMax.y - canvasMin.y) / CanvasHeight * previewRect.height;
            return new Rect(x, y, width, height);
        }

        private static Rect ApplyDrag(Rect rect, Vector2 delta, DragMode mode)
        {
            switch (mode)
            {
                case DragMode.Move:
                    rect.position += delta;
                    break;
                case DragMode.Left:
                    rect.xMin += delta.x;
                    break;
                case DragMode.Right:
                    rect.xMax += delta.x;
                    break;
                case DragMode.Top:
                    rect.yMin += delta.y;
                    break;
                case DragMode.Bottom:
                    rect.yMax += delta.y;
                    break;
                case DragMode.TopLeft:
                    rect.xMin += delta.x;
                    rect.yMin += delta.y;
                    break;
                case DragMode.TopRight:
                    rect.xMax += delta.x;
                    rect.yMin += delta.y;
                    break;
                case DragMode.BottomLeft:
                    rect.xMin += delta.x;
                    rect.yMax += delta.y;
                    break;
                case DragMode.BottomRight:
                    rect.xMax += delta.x;
                    rect.yMax += delta.y;
                    break;
            }

            if (rect.width < 20f)
            {
                rect.width = 20f;
            }

            if (rect.height < 20f)
            {
                rect.height = 20f;
            }

            return rect;
        }

        private static Rect ClampToPreview(Rect rect, Rect previewRect)
        {
            if (rect.width > previewRect.width)
            {
                rect.width = previewRect.width;
            }

            if (rect.height > previewRect.height)
            {
                rect.height = previewRect.height;
            }

            rect.x = Mathf.Clamp(rect.x, previewRect.xMin, previewRect.xMax - rect.width);
            rect.y = Mathf.Clamp(rect.y, previewRect.yMin, previewRect.yMax - rect.height);
            return rect;
        }

        private static DragMode HitTest(Rect rect, Vector2 mousePosition)
        {
            if (GetHandleRect(rect, DragMode.TopLeft).Contains(mousePosition)) return DragMode.TopLeft;
            if (GetHandleRect(rect, DragMode.Top).Contains(mousePosition)) return DragMode.Top;
            if (GetHandleRect(rect, DragMode.TopRight).Contains(mousePosition)) return DragMode.TopRight;
            if (GetHandleRect(rect, DragMode.Left).Contains(mousePosition)) return DragMode.Left;
            if (GetHandleRect(rect, DragMode.Right).Contains(mousePosition)) return DragMode.Right;
            if (GetHandleRect(rect, DragMode.BottomLeft).Contains(mousePosition)) return DragMode.BottomLeft;
            if (GetHandleRect(rect, DragMode.Bottom).Contains(mousePosition)) return DragMode.Bottom;
            if (GetHandleRect(rect, DragMode.BottomRight).Contains(mousePosition)) return DragMode.BottomRight;
            return rect.Contains(mousePosition) ? DragMode.Move : DragMode.None;
        }

        private static Rect GetHandleRect(Rect rect, DragMode mode)
        {
            var half = HandleSize * 0.5f;
            var center = mode switch
            {
                DragMode.TopLeft => new Vector2(rect.xMin, rect.yMin),
                DragMode.Top => new Vector2(rect.center.x, rect.yMin),
                DragMode.TopRight => new Vector2(rect.xMax, rect.yMin),
                DragMode.Left => new Vector2(rect.xMin, rect.center.y),
                DragMode.Right => new Vector2(rect.xMax, rect.center.y),
                DragMode.BottomLeft => new Vector2(rect.xMin, rect.yMax),
                DragMode.Bottom => new Vector2(rect.center.x, rect.yMax),
                DragMode.BottomRight => new Vector2(rect.xMax, rect.yMax),
                _ => rect.center
            };

            return new Rect(center.x - half, center.y - half, HandleSize, HandleSize);
        }

        private static void AddResizeCursors(Rect rect)
        {
            EditorGUIUtility.AddCursorRect(GetHandleRect(rect, DragMode.TopLeft), MouseCursor.ResizeUpLeft);
            EditorGUIUtility.AddCursorRect(GetHandleRect(rect, DragMode.TopRight), MouseCursor.ResizeUpRight);
            EditorGUIUtility.AddCursorRect(GetHandleRect(rect, DragMode.BottomLeft), MouseCursor.ResizeUpRight);
            EditorGUIUtility.AddCursorRect(GetHandleRect(rect, DragMode.BottomRight), MouseCursor.ResizeUpLeft);
            EditorGUIUtility.AddCursorRect(GetHandleRect(rect, DragMode.Left), MouseCursor.ResizeHorizontal);
            EditorGUIUtility.AddCursorRect(GetHandleRect(rect, DragMode.Right), MouseCursor.ResizeHorizontal);
            EditorGUIUtility.AddCursorRect(GetHandleRect(rect, DragMode.Top), MouseCursor.ResizeVertical);
            EditorGUIUtility.AddCursorRect(GetHandleRect(rect, DragMode.Bottom), MouseCursor.ResizeVertical);
        }

        private static void DrawHandle(Rect rect)
        {
            EditorGUI.DrawRect(rect, new Color(0.3f, 0.75f, 1f, 1f));
            DrawBorder(rect, Color.white, 1f);
        }

        private static void DrawBorder(Rect rect, Color color, float thickness)
        {
            EditorGUI.DrawRect(new Rect(rect.xMin, rect.yMin, rect.width, thickness), color);
            EditorGUI.DrawRect(new Rect(rect.xMin, rect.yMax - thickness, rect.width, thickness), color);
            EditorGUI.DrawRect(new Rect(rect.xMin, rect.yMin, thickness, rect.height), color);
            EditorGUI.DrawRect(new Rect(rect.xMax - thickness, rect.yMin, thickness, rect.height), color);
        }

        private static Vector2 ReadVector2(SerializedProperty property, string relativeName, Vector2 fallback)
        {
            var child = property.FindPropertyRelative(relativeName);
            return child != null ? child.vector2Value : fallback;
        }

        private static string GetPreviewText(SerializedProperty textBlockProp)
        {
            var speaker = textBlockProp.FindPropertyRelative("speaker")?.stringValue;
            var content = textBlockProp.FindPropertyRelative("content")?.stringValue;

            if (!string.IsNullOrEmpty(speaker))
            {
                return speaker + ": " + content;
            }

            return string.IsNullOrEmpty(content) ? "(empty text block)" : content;
        }
    }
}
