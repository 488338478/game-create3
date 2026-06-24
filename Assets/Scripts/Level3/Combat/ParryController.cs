using UnityEngine;
using UnityEngine.InputSystem;

namespace GameCreate3.Level3
{
    public sealed class ParryController : MonoBehaviour
    {
        [Header("Parry Settings")]
        [SerializeField] private float parryRadius = 2.5f;
        [SerializeField] private float parryCooldown = 0.3f;
        [SerializeField] private int parryActiveFrames = 12;
        [SerializeField] private LayerMask projectileLayer;

        [Header("VFX")]
        [SerializeField] private GameObject parryBurstPrefab;

        private SideScrollWorkspaceBase workspace;
        private InputAction parryAction;
        private Animator animator;
        private bool isEnabled;
        private float cooldownTimer;
        private int activeFramesLeft;
        private bool parryHit;

        private static readonly int ParryTrigger = Animator.StringToHash("Parry");

        private void Awake()
        {
            workspace = GetComponentInParent<SideScrollWorkspaceBase>(true);
            animator = GetComponentInChildren<Animator>();

            parryAction = new InputAction("Parry", InputActionType.Button);
            parryAction.AddBinding("<Keyboard>/f");
            parryAction.AddBinding("<Gamepad>/buttonNorth");
            parryAction.Enable();
        }

        private void OnDestroy()
        {
            parryAction?.Disable();
            parryAction?.Dispose();
        }

        private void Update()
        {
            if (!isEnabled) return;

            if (cooldownTimer > 0f)
            {
                cooldownTimer -= Time.deltaTime;
                return;
            }

            if (parryAction.WasPressedThisFrame())
            {
                activeFramesLeft = parryActiveFrames;
                parryHit = false;
                animator?.SetTrigger(ParryTrigger);
            }

            if (activeFramesLeft > 0)
            {
                activeFramesLeft--;
                CheckParryHits();

                if (activeFramesLeft <= 0)
                {
                    cooldownTimer = parryCooldown;
                    if (parryHit)
                    {
                        workspace?.RaiseWorkspaceEvent(Level3Events.ParrySuccess);
                        if (parryBurstPrefab != null)
                        {
                            var burst = Instantiate(parryBurstPrefab, transform.position, Quaternion.identity);
                            Destroy(burst, 0.5f);
                        }
                    }
                }
            }
        }

        // --- WorkspaceEventRouter 调用的 public 入口 ---

        public void OnPhase2() => isEnabled = true;
        public void OnPhase3() => isEnabled = false;

        // --- 内部 ---

        private void CheckParryHits()
        {
            var hits = Physics2D.OverlapCircleAll(transform.position, parryRadius, projectileLayer);

            foreach (var hit in hits)
            {
                var projectile = hit.GetComponent<VerbalAttackProjectile>();
                if (projectile != null && !projectile.IsDeflected)
                {
                    var dir = ((Vector2)hit.transform.position - (Vector2)transform.position).normalized;
                    projectile.Deflect(dir);
                    parryHit = true;
                }
            }
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            if (!isEnabled) return;
            Gizmos.color = new Color(0, 1, 1, 0.3f);
            Gizmos.DrawWireSphere(transform.position, parryRadius);
        }
#endif
    }
}
