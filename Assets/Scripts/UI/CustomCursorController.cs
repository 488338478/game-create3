using UnityEngine;

namespace GameCreate3.UI
{
    public sealed class CustomCursorController : MonoBehaviour
    {
        [Header("Textures")]
        [SerializeField] private Texture2D standardCursor;
        [SerializeField] private Texture2D pressedCursor;
        [SerializeField] private Texture2D moveCursor;

        [Header("Settings")]
        [SerializeField] private Vector2 hotspot = Vector2.zero;
        [SerializeField] private CursorMode cursorMode = CursorMode.Auto;
        [SerializeField] private float dragThresholdPixels = 8f;
        [SerializeField] private bool persistAcrossScenes = true;

        private Vector2 pointerDownPosition;
        private CursorState currentState = CursorState.None;

        private enum CursorState
        {
            None,
            Standard,
            Pressed,
            Move
        }

        private void Awake()
        {
            if (persistAcrossScenes)
            {
                DontDestroyOnLoad(gameObject);
            }
        }

        private void OnEnable()
        {
            SetCursor(CursorState.Standard);
        }

        private void Update()
        {
            if (Input.GetMouseButtonDown(0))
            {
                pointerDownPosition = Input.mousePosition;
                SetCursor(CursorState.Pressed);
                return;
            }

            if (Input.GetMouseButton(0))
            {
                var dragDistance = ((Vector2)Input.mousePosition - pointerDownPosition).magnitude;
                SetCursor(dragDistance >= dragThresholdPixels ? CursorState.Move : CursorState.Pressed);
                return;
            }

            if (Input.GetMouseButtonUp(0))
            {
                SetCursor(CursorState.Standard);
                return;
            }

            SetCursor(CursorState.Standard);
        }

        private void OnDisable()
        {
            Cursor.SetCursor(null, Vector2.zero, cursorMode);
            currentState = CursorState.None;
        }

        private void SetCursor(CursorState state)
        {
            if (currentState == state)
            {
                return;
            }

            var texture = state switch
            {
                CursorState.Pressed => pressedCursor != null ? pressedCursor : standardCursor,
                CursorState.Move => moveCursor != null ? moveCursor : standardCursor,
                _ => standardCursor
            };

            Cursor.SetCursor(texture, hotspot, cursorMode);
            currentState = state;
        }

#if UNITY_EDITOR
        private void Reset()
        {
            standardCursor = UnityEditor.AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Arts/UI/鼠标图标/mouse_standard.png");
            pressedCursor = UnityEditor.AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Arts/UI/鼠标图标/mouse_pre.png");
            moveCursor = UnityEditor.AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Arts/UI/鼠标图标/mouse_move.png");
        }
#endif
    }
}
