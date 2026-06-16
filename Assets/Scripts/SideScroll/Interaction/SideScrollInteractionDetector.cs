using System;
using UnityEngine;

namespace GameCreate3
{
    public sealed class SideScrollInteractionDetector : MonoBehaviour
    {
        [Tooltip("用作交互探测形状的 Collider2D（必须 isTrigger=true）。留空则在 Awake 时自动找自身第一个 isTrigger 的 Collider2D。")]
        [SerializeField] private Collider2D detectCollider;
        [SerializeField] private LayerMask interactableMask = ~0;
        [Tooltip("打开后：Gizmos 画出 detect box / 扫到的全部 collider / 当前 hover；每次扫到新候选时打 log。仅诊断用。")]
        [SerializeField] private bool debugDraw;

        private readonly Collider2D[] results = new Collider2D[8];
        private ContactFilter2D filter;
        private bool filterReady;
        private bool warnedMissingCollider;
        private int lastHitCount;
        private readonly Collider2D[] lastHits = new Collider2D[8];
        private Collider2D lastHoverCollider;

        public ISideScrollInteractable CurrentHover { get; private set; }

        public event Action<ISideScrollInteractable, ISideScrollInteractable> HoverChanged;

        public static event Action<SideScrollInteractionDetector, ISideScrollInteractable, ISideScrollInteractable> HoverGlobalChanged;

        public Collider2D DetectCollider => detectCollider;

        private void Awake()
        {
            EnsureDetectCollider();
        }

        public void SetInteractableMask(LayerMask mask)
        {
            interactableMask = mask;
            filterReady = false;
        }

        public void SetDetectCollider(Collider2D collider)
        {
            detectCollider = collider;
        }

        public void ProcessInteraction(GameObject interactor, bool interactPressed)
        {
            if (!interactPressed)
            {
                return;
            }

            UpdateHover(interactor);

            var target = CurrentHover;
            if (target != null && target.CanInteract(interactor))
            {
                target.Interact(interactor);
                GameCreate3.Core.GameAudioService.Instance?.PlaySFX("SFX_Interact");
            }
        }

        private void Update()
        {
            UpdateHover(gameObject);
        }

        private void OnDisable()
        {
            SetHover(null);
        }

        private void EnsureDetectCollider()
        {
            if (detectCollider != null) return;

            // fallback：找自身上第一个 isTrigger=true 的 Collider2D，避免误取到物理 collider。
            foreach (var c in GetComponents<Collider2D>())
            {
                if (c != null && c.isTrigger)
                {
                    detectCollider = c;
                    break;
                }
            }
        }

        private void EnsureFilter()
        {
            if (filterReady) return;
            filter = new ContactFilter2D
            {
                useLayerMask = true,
                useTriggers = true,         // 关键：interactable 的 BoxCollider2D 通常也是 trigger，必须开
            };
            filter.SetLayerMask(interactableMask == 0 ? ~0 : interactableMask);
            filterReady = true;
        }

        private void UpdateHover(GameObject interactor)
        {
            if (detectCollider == null)
            {
                EnsureDetectCollider();
                if (detectCollider == null)
                {
                    if (!warnedMissingCollider)
                    {
                        warnedMissingCollider = true;
                        Debug.LogWarning("[SideScrollInteractionDetector] 找不到 isTrigger 的 Collider2D，hover 探测将一直为空。请在玩家身上加一个 BoxCollider2D(isTrigger=true) 并赋给 detectCollider 字段。", this);
                    }
                    // silent return：不广播 SetHover(null)，避免同 GameObject 上多份 detector 共存时
                    // 没 collider 的那份反复覆盖有 collider 那份的 hover 状态。
                    return;
                }
            }

            EnsureFilter();

            var count = detectCollider.OverlapCollider(filter, results);

            ISideScrollInteractable best = null;
            Collider2D bestCollider = null;
            var bestSqr = float.PositiveInfinity;
            var origin = (Vector2)detectCollider.bounds.center;
            for (var i = 0; i < count; i++)
            {
                var candidate = results[i];
                if (candidate == null || !candidate.TryGetComponent<ISideScrollInteractable>(out var interactable))
                {
                    continue;
                }
                if (!interactable.CanInteract(interactor))
                {
                    continue;
                }

                // 多个候选时取离玩家中心最近的，保证 hover 唯一、按 E 触发的就是亮提示的那个。
                var sqr = ((Vector2)candidate.bounds.ClosestPoint(origin) - origin).sqrMagnitude;
                if (sqr < bestSqr)
                {
                    bestSqr = sqr;
                    best = interactable;
                    bestCollider = candidate;
                }
            }

            if (debugDraw)
            {
                CaptureDebugHits(count);
                lastHoverCollider = bestCollider;
            }

            SetHover(best);
        }

        private void CaptureDebugHits(int count)
        {
            lastHitCount = Mathf.Min(count, lastHits.Length);
            for (var i = 0; i < lastHits.Length; i++)
            {
                lastHits[i] = i < lastHitCount ? results[i] : null;
            }
        }

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            if (!debugDraw) return;
            if (detectCollider == null) return;

            // 玩家 detect box 几何位置（白）
            var detectBounds = detectCollider.bounds;
            Gizmos.color = Color.white;
            Gizmos.DrawWireCube(detectBounds.center, detectBounds.size);

            // 当前扫到的全部 candidate（黄）
            Gizmos.color = Color.yellow;
            for (var i = 0; i < lastHitCount; i++)
            {
                var c = lastHits[i];
                if (c == null) continue;
                var b = c.bounds;
                Gizmos.DrawWireCube(b.center, b.size);

                // 从 detect 中心到该 candidate 中心连线
                Gizmos.DrawLine(detectBounds.center, b.center);
            }

            // 当前 hover（绿，加粗一圈）
            if (lastHoverCollider != null)
            {
                Gizmos.color = Color.green;
                var b = lastHoverCollider.bounds;
                Gizmos.DrawWireCube(b.center, b.size);
                Gizmos.DrawWireCube(b.center, b.size * 1.03f);
            }
        }
#endif

        private void SetHover(ISideScrollInteractable next)
        {
            var prev = CurrentHover;
            if (ReferenceEquals(prev, next))
            {
                return;
            }

            CurrentHover = next;
            HoverChanged?.Invoke(prev, next);
            HoverGlobalChanged?.Invoke(this, prev, next);
        }
    }
}
