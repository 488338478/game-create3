using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using GameCreate3;

namespace GameCreate3.EditorTools
{
    /// <summary>
    /// 把指定 prefab 里所有"散装"InteractTrigger GameObject
    /// 替换为 Atoms/InteractTrigger.prefab 的实例，并把原数据
    /// （interactId / prompt / oneShot / Transform / Layer /
    ///  SpriteRenderer / BoxCollider2D 的差异）以 prefab override
    /// 的形式保留下来。
    ///
    /// 菜单：GameCreate3 → SideScroll → Convert InteractTriggers To Atom Instances
    /// </summary>
    public static class InteractTriggerAtomConverter
    {
        private const string AtomPrefabPath = "Assets/Prefabs/SideScroll/Atoms/InteractTrigger.prefab";

        [MenuItem("GameCreate3/SideScroll/Convert InteractTriggers To Atom Instances")]
        public static void ConvertDualWorldWorkspace()
        {
            ConvertPrefab("Assets/Prefabs/DualWorld/DualWorldWorkspace.prefab");
        }

        public static void ConvertPrefab(string prefabPath)
        {
            if (Application.isPlaying)
            {
                Debug.LogError("[InteractTriggerAtomConverter] Cannot run while in Play mode.");
                return;
            }

            var atom = AssetDatabase.LoadAssetAtPath<GameObject>(AtomPrefabPath);
            if (atom == null)
            {
                Debug.LogError($"[InteractTriggerAtomConverter] Atom prefab not found at {AtomPrefabPath}");
                return;
            }

            var root = PrefabUtility.LoadPrefabContents(prefabPath);
            if (root == null)
            {
                Debug.LogError($"[InteractTriggerAtomConverter] Cannot load prefab contents at {prefabPath}");
                return;
            }

            try
            {
                var triggers = root.GetComponentsInChildren<InteractTrigger>(true);
                Debug.Log($"[InteractTriggerAtomConverter] Found {triggers.Length} InteractTrigger(s) in {prefabPath}");

                int converted = 0;
                foreach (var trigger in triggers)
                {
                    if (trigger == null) continue;

                    var go = trigger.gameObject;

                    // 已经是 atom 实例就跳过
                    if (PrefabUtility.GetCorrespondingObjectFromSource(go) != null)
                    {
                        Debug.Log($"  - Skip {GetPath(go.transform)} (already a prefab instance).");
                        continue;
                    }

                    // 子节点不处理（atom 不允许 trigger 当父节点）
                    if (go.transform.childCount > 0)
                    {
                        Debug.LogWarning($"  - Skip {GetPath(go.transform)} (has children, manual review needed).");
                        continue;
                    }

                    var snap = Snapshot.Capture(go);
                    var parent = go.transform.parent;
                    int sibling = go.transform.GetSiblingIndex();

                    Object.DestroyImmediate(go);

                    var instance = (GameObject)PrefabUtility.InstantiatePrefab(atom, parent);
                    instance.transform.SetSiblingIndex(sibling);
                    snap.ApplyTo(instance);

                    converted++;
                }

                if (converted > 0)
                {
                    PrefabUtility.SaveAsPrefabAsset(root, prefabPath, out var success);
                    if (!success)
                    {
                        Debug.LogError("[InteractTriggerAtomConverter] SaveAsPrefabAsset failed.");
                    }
                    else
                    {
                        Debug.Log($"[InteractTriggerAtomConverter] Converted {converted} InteractTrigger(s) → atom instances and saved {prefabPath}.");
                    }
                }
                else
                {
                    Debug.Log("[InteractTriggerAtomConverter] Nothing to convert.");
                }
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static string GetPath(Transform t)
        {
            var stack = new Stack<string>();
            while (t != null)
            {
                stack.Push(t.name);
                t = t.parent;
            }
            return string.Join("/", stack);
        }

        private struct Snapshot
        {
            public string name;
            public int layer;
            public bool active;
            public Vector3 localPos;
            public Quaternion localRot;
            public Vector3 localScale;

            public string interactId;
            public string prompt;
            public bool oneShot;

            public bool hasSprite;
            public Sprite sprite;
            public Color spriteColor;
            public int sortingLayerID;
            public int sortingOrder;
            public bool flipX;
            public bool flipY;
            public SpriteDrawMode drawMode;
            public Vector2 spriteSize;

            public bool hasCollider;
            public Vector2 colliderSize;
            public Vector2 colliderOffset;
            public bool isTrigger;

            public static Snapshot Capture(GameObject go)
            {
                var s = new Snapshot
                {
                    name = go.name,
                    layer = go.layer,
                    active = go.activeSelf,
                    localPos = go.transform.localPosition,
                    localRot = go.transform.localRotation,
                    localScale = go.transform.localScale,
                };

                var trig = go.GetComponent<InteractTrigger>();
                if (trig != null)
                {
                    var so = new SerializedObject(trig);
                    s.prompt = so.FindProperty("prompt").stringValue;
                    s.interactId = so.FindProperty("interactId").stringValue;
                    s.oneShot = so.FindProperty("oneShot").boolValue;
                }

                var sr = go.GetComponent<SpriteRenderer>();
                if (sr != null)
                {
                    s.hasSprite = true;
                    s.sprite = sr.sprite;
                    s.spriteColor = sr.color;
                    s.sortingLayerID = sr.sortingLayerID;
                    s.sortingOrder = sr.sortingOrder;
                    s.flipX = sr.flipX;
                    s.flipY = sr.flipY;
                    s.drawMode = sr.drawMode;
                    s.spriteSize = sr.size;
                }

                var box = go.GetComponent<BoxCollider2D>();
                if (box != null)
                {
                    s.hasCollider = true;
                    s.colliderSize = box.size;
                    s.colliderOffset = box.offset;
                    s.isTrigger = box.isTrigger;
                }

                return s;
            }

            public void ApplyTo(GameObject instance)
            {
                instance.name = name;
                instance.layer = layer;
                instance.SetActive(active);
                instance.transform.localPosition = localPos;
                instance.transform.localRotation = localRot;
                instance.transform.localScale = localScale;

                var trig = instance.GetComponent<InteractTrigger>();
                if (trig != null)
                {
                    var so = new SerializedObject(trig);
                    so.FindProperty("prompt").stringValue = prompt ?? string.Empty;
                    so.FindProperty("interactId").stringValue = interactId ?? string.Empty;
                    so.FindProperty("oneShot").boolValue = oneShot;
                    so.ApplyModifiedPropertiesWithoutUndo();
                }

                if (hasSprite)
                {
                    var sr = instance.GetComponent<SpriteRenderer>();
                    if (sr != null)
                    {
                        sr.sprite = sprite;
                        sr.color = spriteColor;
                        sr.sortingLayerID = sortingLayerID;
                        sr.sortingOrder = sortingOrder;
                        sr.flipX = flipX;
                        sr.flipY = flipY;
                        sr.drawMode = drawMode;
                        sr.size = spriteSize;
                    }
                }

                if (hasCollider)
                {
                    var box = instance.GetComponent<BoxCollider2D>();
                    if (box != null)
                    {
                        box.size = colliderSize;
                        box.offset = colliderOffset;
                        box.isTrigger = isTrigger;
                    }
                }
            }
        }
    }
}
