using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

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
        [SerializeField] private string[] pressedHoverTags = System.Array.Empty<string>();
        [SerializeField] private string[] moveHoverTags = System.Array.Empty<string>();
        [SerializeField] private Camera raycastCamera;
        [SerializeField] private float raycastDistance = 100f;
        [SerializeField] private bool persistAcrossScenes = true;

        private readonly List<RaycastResult> uiRaycastResults = new List<RaycastResult>();
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
            if (IsPointerOverTaggedObject(moveHoverTags))
            {
                SetCursor(CursorState.Move);
                return;
            }

            if (IsPointerOverTaggedObject(pressedHoverTags))
            {
                SetCursor(CursorState.Pressed);
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

        private bool IsPointerOverTaggedObject(IReadOnlyList<string> tags)
        {
            if (tags == null || tags.Count == 0)
            {
                return false;
            }

            return IsPointerOverTaggedUi(tags) || IsPointerOverTaggedWorldObject(tags);
        }

        private bool IsPointerOverTaggedUi(IReadOnlyList<string> tags)
        {
            if (EventSystem.current == null)
            {
                return false;
            }

            uiRaycastResults.Clear();
            var pointerEventData = new PointerEventData(EventSystem.current)
            {
                position = Input.mousePosition
            };

            EventSystem.current.RaycastAll(pointerEventData, uiRaycastResults);
            foreach (var result in uiRaycastResults)
            {
                if (HasAnyTag(result.gameObject, tags))
                {
                    return true;
                }
            }

            return false;
        }

        private bool IsPointerOverTaggedWorldObject(IReadOnlyList<string> tags)
        {
            var cameraToUse = raycastCamera != null ? raycastCamera : Camera.main;
            if (cameraToUse == null)
            {
                return false;
            }

            var ray = cameraToUse.ScreenPointToRay(Input.mousePosition);
            var hit2D = Physics2D.GetRayIntersection(ray, raycastDistance);
            if (hit2D.collider != null && HasAnyTag(hit2D.collider.gameObject, tags))
            {
                return true;
            }

            return Physics.Raycast(ray, out var hit3D, raycastDistance) && HasAnyTag(hit3D.collider.gameObject, tags);
        }

        private static bool HasAnyTag(GameObject target, IReadOnlyList<string> tags)
        {
            if (target == null)
            {
                return false;
            }

            var current = target.transform;
            while (current != null)
            {
                foreach (var tag in tags)
                {
                    if (!string.IsNullOrWhiteSpace(tag) && current.tag == tag)
                    {
                        return true;
                    }
                }

                current = current.parent;
            }

            return false;
        }

#if UNITY_EDITOR
        private void Reset()
        {
            standardCursor = UnityEditor.AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Arts/_Shared/UI/Cursor/mouse_standard.png");
            pressedCursor = UnityEditor.AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Arts/_Shared/UI/Cursor/mouse_pre.png");
            moveCursor = UnityEditor.AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Arts/_Shared/UI/Cursor/mouse_move.png");
        }
#endif
    }
}
