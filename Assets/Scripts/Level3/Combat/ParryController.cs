using UnityEngine;

namespace GameCreate3.Level3
{
    public sealed class ParryController : MonoBehaviour
    {
        [Header("Parry Settings")]
        [SerializeField] private float parryRadius = 2.5f;
        [SerializeField] private float parryCooldown = 0.3f;
        [SerializeField] private LayerMask projectileLayer;

        [Header("VFX")]
        [SerializeField] private GameObject parryBurstPrefab;

        private SideScrollWorkspaceBase workspace;
        private CharacterInputProxy inputProxy;
        private bool isEnabled;
        private float cooldownTimer;

        private void Awake()
        {
            workspace = GetComponentInParent<SideScrollWorkspaceBase>(true);
            inputProxy = GetComponent<CharacterInputProxy>();
        }

        private void Update()
        {
            if (!isEnabled) return;
            if (cooldownTimer > 0f)
            {
                cooldownTimer -= Time.deltaTime;
                return;
            }
            if (inputProxy != null && inputProxy.InteractPressed)
                TryParry();
        }

        // --- WorkspaceEventRouter 调用的 public 入口 ---

        public void OnPhase2() => isEnabled = true;
        public void OnPhase3() => isEnabled = false;

        // --- 内部 ---

        private void TryParry()
        {
            cooldownTimer = parryCooldown;
            var hits = Physics2D.OverlapCircleAll(transform.position, parryRadius, projectileLayer);
            var deflectedCount = 0;

            foreach (var hit in hits)
            {
                var projectile = hit.GetComponent<VerbalAttackProjectile>();
                if (projectile != null && !projectile.IsDeflected)
                {
                    projectile.Deflect();
                    deflectedCount++;
                }
            }

            if (deflectedCount > 0)
            {
                workspace?.RaiseWorkspaceEvent(Level3Events.ParrySuccess);
                if (parryBurstPrefab != null)
                {
                    var burst = Instantiate(parryBurstPrefab, transform.position, Quaternion.identity);
                    Destroy(burst, 0.5f);
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
