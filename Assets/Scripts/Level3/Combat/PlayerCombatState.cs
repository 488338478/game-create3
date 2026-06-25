using System.Collections;
using UnityEngine;

namespace GameCreate3.Level3
{
    public sealed class PlayerCombatState : MonoBehaviour
    {
        [Header("Health")]
        [SerializeField] private int maxHits = 5;

        [Header("Animation")]
        [SerializeField] private Animator animator;
        [SerializeField] private string hitTriggerName = "Death";
        [SerializeField] private int hitAnimLayerIndex = 4;
        [SerializeField] private string hitAnimStateName = "bear_death";

        [Header("Hit Stun")]
        [SerializeField] private float postAnimInvincibility = 0.5f;

        [Header("Visual Feedback")]
        [SerializeField] private float blinkInterval = 0.1f;

        public int CurrentHits { get; private set; }
        public bool IsInvincible { get; private set; }
        public bool IsDefeated { get; private set; }

        private SideScrollWorkspaceBase workspace;
        private SideScrollCharacterControllerBase playerController;
        private SpriteRenderer spriteRenderer;
        private Coroutine hitRoutine;

        private int hitTriggerHash;

        private void Awake()
        {
            workspace = GetComponentInParent<SideScrollWorkspaceBase>(true);
            playerController = GetComponent<SideScrollCharacterControllerBase>();
            if (animator == null)
                animator = GetComponentInChildren<Animator>(true);
            spriteRenderer = GetComponentInChildren<SpriteRenderer>(true);
            CurrentHits = maxHits;
            hitTriggerHash = Animator.StringToHash(hitTriggerName);
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
            animator?.SetTrigger(hitTriggerHash);

            yield return null;
            float animDuration = 0.72f;
            if (animator != null)
            {
                float timeout = 0.5f;
                while (!animator.GetCurrentAnimatorStateInfo(hitAnimLayerIndex).IsName(hitAnimStateName) && timeout > 0f)
                {
                    timeout -= Time.deltaTime;
                    yield return null;
                }
                var stateInfo = animator.GetCurrentAnimatorStateInfo(hitAnimLayerIndex);
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

            yield return new WaitForSeconds(postAnimInvincibility);

            IsInvincible = false;
            hitRoutine = null;
        }
    }
}
