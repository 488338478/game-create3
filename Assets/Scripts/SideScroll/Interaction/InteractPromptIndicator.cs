using UnityEngine;

namespace GameCreate3
{
    [DisallowMultipleComponent]
    public sealed class InteractPromptIndicator : MonoBehaviour
    {
        [SerializeField] private GameObject promptVisual;
        [SerializeField] private Vector3 offset = new Vector3(0f, 0.7f, 0f);
        [SerializeField] private float floatAmplitude = 0.08f;
        [SerializeField] private float floatSpeed = 2.5f;

        private Vector3 baseLocalPos;
        private bool visible;
        private ISideScrollInteractable ownInteractable;

        private static Sprite placeholderSprite;

        private void Awake()
        {
            EnsurePromptVisual();
            if (promptVisual == null) return;

            baseLocalPos = offset;
            promptVisual.transform.localPosition = baseLocalPos;
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
            baseLocalPos = ComputeBaseLocalPos();
            promptVisual.transform.localPosition = baseLocalPos;
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
            var y = baseLocalPos.y + Mathf.Sin(Time.time * floatSpeed) * floatAmplitude;
            promptVisual.transform.localPosition = new Vector3(baseLocalPos.x, y, baseLocalPos.z);
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
            if (promptVisual != null) promptVisual.SetActive(true);
        }

        private void Hide()
        {
            if (!visible) return;
            visible = false;
            if (promptVisual != null) promptVisual.SetActive(false);
        }

        private Vector3 ComputeBaseLocalPos()
        {
            // Offset 以「碰撞体几何中心」为基准，而非 Indicator 自身原点，
            // 这样无论 Collider2D.offset 怎么设，Prompt 都会浮在物体几何中心上方。
            var col = GetComponent<Collider2D>()
                ?? (ownInteractable as Component)?.GetComponent<Collider2D>()
                ?? GetComponentInParent<Collider2D>()
                ?? GetComponentInChildren<Collider2D>();
            if (col == null) return offset;

            var centerLocal = transform.InverseTransformPoint(col.bounds.center);
            return centerLocal + offset;
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
