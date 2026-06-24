using System.Collections;
using UnityEngine;

namespace GameCreate3.Level3
{
    public sealed class PlayerCombatState : MonoBehaviour
    {
        [Header("Health")]
        [SerializeField] private int maxHits = 5;

        [Header("Hit Stun")]
        [SerializeField] private float postAnimInvincibility = 0.5f;

        [Header("Visual Feedback")]
        [SerializeField] private float blinkInterval = 0.1f;

        public int CurrentHits { get; private set; }
        public bool IsInvincible { get; private set; }
        public bool IsDefeated { get; private set; }

        private SideScrollWorkspaceBase workspace;
        private SideScrollCharacterControllerBase playerController;
        private Animator animator;
        private SpriteRenderer spriteRenderer;
        private Coroutine hitRoutine;

        private static readonly int DeathTrigger = Animator.StringToHash("Death");
        private const int DeathLayerIndex = 4;

        private void Awake()
        {
            workspace = GetComponentInParent<SideScrollWorkspaceBase>(true);
            playerController = GetComponent<SideScrollCharacterControllerBase>();
            animator = GetComponentInChildren<Animator>(true);
            spriteRenderer = GetComponentInChildren<SpriteRenderer>(true);
            CurrentHits = maxHits;
        }

        public void OnProjectileHit(int damage = 1)
        {
            if (IsDefeated) return;

            workspace?.RaiseWorkspaceEvent(Level3Events.PlayerHit);

            if (IsInvincible) return;

            CurrentHits -= damage;
            if (CurrentHits <= 0)
                CurrentHits = 1;

            if (hitRoutine != null)
                StopCoroutine(hitRoutine);
            hitRoutine = StartCoroutine(HitStunCoroutine());
        }

        private IEnumerator HitStunCoroutine()
        {
            IsInvincible = true;
            playerController?.SetInputEnabled(false);
            animator?.SetTrigger(DeathTrigger);

            yield return null;
            float animDuration = 0.72f;
            if (animator != null)
            {
                float timeout = 0.5f;
                while (!animator.GetCurrentAnimatorStateInfo(DeathLayerIndex).IsName("bear_death") && timeout > 0f)
                {
                    timeout -= Time.deltaTime;
                    yield return null;
                }
                var stateInfo = animator.GetCurrentAnimatorStateInfo(DeathLayerIndex);
                animDuration = stateInfo.length;
            }

            // 闪烁和动画同步，动画结束即解除一切
            float elapsed = 0f;
            bool visible = true;
            while (elapsed < animDuration)
            {
                visible = !visible;
                if (spriteRenderer != null)
                    spriteRenderer.enabled = visible;
                yield return new WaitForSeconds(blinkInterval);
                elapsed += blinkInterval;
            }
            if (spriteRenderer != null)
                spriteRenderer.enabled = true;

            playerController?.SetInputEnabled(true);
            IsInvincible = false;
            hitRoutine = null;
        }
    }
}
