using UnityEngine;

namespace GameCreate3
{
    [DisallowMultipleComponent]
    public sealed class InteractPromptIndicator : MonoBehaviour
    {
        [SerializeField] private GameObject promptVisual;
        [Tooltip("Prompt 的定位基准物体。不填则用本物体（按碰撞体几何中心）；填了则浮在指定物体上方。")]
        [SerializeField] private Transform targetObject;
        [SerializeField] private Vector3 offset = new Vector3(0f, 0.7f, 0f);
        [SerializeField] private float floatAmplitude = 0.08f;
        [SerializeField] private float floatSpeed = 2.5f;
        [Tooltip("锁定 Prompt 的世界缩放，使其不受父物体缩放影响。")]
        [SerializeField] private bool maintainConstantScale = true;
        [Tooltip("Prompt 缩放系数，在设计缩放基础上整体放大/缩小，可运行时实时调整。")]
        [SerializeField] private float promptScale = 0.3f;

        private Vector3 promptDesignScale = Vector3.one;
        private bool visible;
        private ISideScrollInteractable ownInteractable;

        private static Sprite placeholderSprite;

        private void Awake()
        {
            EnsurePromptVisual();
            if (promptVisual == null) return;

            promptDesignScale = promptVisual.transform.localScale;
            promptVisual.transform.rotation = Quaternion.identity;
            promptVisual.SetActive(false);

            // 同 GameObject / 父链 / 子链都允许，方便 Indicator 挂在装饰节点上时仍能找到归属的 interactable。
            ownInteractable = GetComponent<ISideScrollInteractable>()
                ?? GetComponentInParent<ISideScrollInteractable>()
                ?? GetComponentInChildren<ISideScrollInteractable>();
        }

        private void Start()
        {
            if (promptVisual == null) return;
            promptVisual.transform.position = GetBasisWorldCenter() + offset;
        }

        private void OnEnable()
        {
            SideScrollInteractionDetector.HoverGlobalChanged += OnHoverChanged;
        }

        private void OnDisable()
        {
            SideScrollInteractionDetector.HoverGlobalChanged -= OnHoverChanged;
            Hide();
        }

        private void Update()
        {
            if (!visible || promptVisual == null) return;
            var floatY = Mathf.Sin(Time.time * floatSpeed) * floatAmplitude;
            promptVisual.transform.position = GetBasisWorldCenter() + offset + new Vector3(0f, floatY, 0f);
        }

        private void LateUpdate()
        {
            if (!visible) return;
            ApplyConstantScale();
        }

        // 用父链 lossyScale 反向补偿 localScale，让 Prompt 的世界缩放锁定在设计值，不被父物体缩放放大/缩小。
        private void ApplyConstantScale()
        {
            if (promptVisual == null) return;
            var targetScale = promptDesignScale * promptScale;

            if (!maintainConstantScale)
            {
                // 不锁世界缩放时，promptScale 直接作用在 localScale 上（仍跟随父物体）。
                promptVisual.transform.localScale = targetScale;
                return;
            }

            var parent = promptVisual.transform.parent;
            var parentScale = parent != null ? parent.lossyScale : Vector3.one;
            promptVisual.transform.localScale = new Vector3(
                SafeDivide(targetScale.x, parentScale.x),
                SafeDivide(targetScale.y, parentScale.y),
                SafeDivide(targetScale.z, parentScale.z));
        }

        private static float SafeDivide(float value, float divisor)
        {
            return Mathf.Approximately(divisor, 0f) ? value : value / divisor;
        }

        private void OnHoverChanged(SideScrollInteractionDetector detector, ISideScrollInteractable prev, ISideScrollInteractable curr)
        {
            if (ownInteractable == null) return;

            if (ReferenceEquals(curr, ownInteractable))
            {
                Show();
            }
            else if (ReferenceEquals(prev, ownInteractable))
            {
                Hide();
            }
        }

        private void Show()
        {
            if (visible) return;
            visible = true;
            if (promptVisual != null)
            {
                promptVisual.SetActive(true);
                ApplyConstantScale();
            }
        }

        private void Hide()
        {
            if (!visible) return;
            visible = false;
            if (promptVisual != null) promptVisual.SetActive(false);
        }

        // 定位基准的世界坐标中心。
        // - 填了 targetObject：以它（有碰撞体取几何中心，否则取 position）为基准。
        // - 没填：维持原行为，以本物体 / 所属 interactable 的碰撞体几何中心为基准。
        // offset 在此基础上作为世界空间偏移，使 Prompt 不受父物体缩放影响而被压扁/拉伸。
        private Vector3 GetBasisWorldCenter()
        {
            var col = ResolveBasisCollider(out var basis);
            return col != null ? col.bounds.center : basis.position;
        }

        private Collider2D ResolveBasisCollider(out Transform basis)
        {
            if (targetObject != null)
            {
                basis = targetObject;
                return targetObject.GetComponent<Collider2D>()
                    ?? targetObject.GetComponentInChildren<Collider2D>();
            }

            basis = transform;
            return GetComponent<Collider2D>()
                ?? (ownInteractable as Component)?.GetComponent<Collider2D>()
                ?? GetComponentInParent<Collider2D>()
                ?? GetComponentInChildren<Collider2D>();
        }

        private void EnsurePromptVisual()
        {
            // promptVisual 字段支持两种用法：
            //   A) 直接拖入一个 prefab 资源（asset，不在场景里） → 这里 Instantiate 一份作为 indicator 子物体。
            //   B) 直接拖入场景里某个 GameObject → 直接当作 visual 引用。要求是 indicator 自身后代，否则会被多份共用。
            if (promptVisual != null)
            {
                if (!promptVisual.scene.IsValid())
                {
                    // prefab asset：实例化一份当作自己的子物体，原引用替换为实例
                    var prefabAsset = promptVisual;
                    promptVisual = Instantiate(prefabAsset, transform);
                    promptVisual.name = prefabAsset.name;
                }
                else if (!promptVisual.transform.IsChildOf(transform))
                {
                    Debug.LogWarning($"[InteractPromptIndicator] '{name}' 的 promptVisual ({promptVisual.name}) 不是自身后代，可能被其他 Indicator 抢用，已 fallback 到自动 placeholder。", this);
                    promptVisual = null;
                }
            }

            if (promptVisual != null) return;

            var existing = transform.Find("Prompt") ?? transform.Find("InteractPrompt");
            if (existing != null)
            {
                promptVisual = existing.gameObject;
                return;
            }

            promptVisual = CreatePlaceholderVisual();
        }

        private GameObject CreatePlaceholderVisual()
        {
            var go = new GameObject("Prompt");
            go.transform.SetParent(transform, false);
            go.transform.localScale = Vector3.one * 0.25f;

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = GetPlaceholderSprite();
            sr.color = new Color(1f, 0.92f, 0.4f, 0.95f);
            sr.sortingOrder = 100;

            return go;
        }

        private static Sprite GetPlaceholderSprite()
        {
            if (placeholderSprite != null) return placeholderSprite;
            var tex = Texture2D.whiteTexture;
            placeholderSprite = Sprite.Create(tex, new Rect(0f, 0f, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100f);
            return placeholderSprite;
        }
    }
}
