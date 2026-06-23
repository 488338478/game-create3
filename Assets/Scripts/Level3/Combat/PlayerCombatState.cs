using System.Collections;
using UnityEngine;

namespace GameCreate3.Level3
{
    public sealed class PlayerCombatState : MonoBehaviour
    {
        [Header("Health")]
        [SerializeField] private int maxHits = 5;
        [SerializeField] private float invincibilityDuration = 1.5f;

        [Header("Visual Feedback")]
        [SerializeField] private float blinkInterval = 0.1f;

        public int CurrentHits { get; private set; }
        public bool IsInvincible { get; private set; }
        public bool IsDefeated { get; private set; }

        private SideScrollWorkspaceBase workspace;
        private SpriteRenderer spriteRenderer;
        private Coroutine invincibilityRoutine;

        private void Awake()
        {
            workspace = GetComponentInParent<SideScrollWorkspaceBase>(true);
            spriteRenderer = GetComponentInChildren<SpriteRenderer>(true);
            CurrentHits = maxHits;
        }

        /// <summary>
        /// 由 VerbalAttackProjectile 碰撞时调用。
        /// </summary>
        public void TakeDamage(int damage = 1)
        {
            if (IsDefeated) return;
            if (IsInvincible) return;

            CurrentHits -= damage;

            if (CurrentHits <= 0)
            {
                CurrentHits = 0;
                IsDefeated = true;
                workspace?.RaiseWorkspaceEvent(Level3Events.PlayerDefeated);
                return;
            }

            workspace?.RaiseWorkspaceEvent(Level3Events.PlayerHit);

            if (invincibilityRoutine != null)
                StopCoroutine(invincibilityRoutine);
            invincibilityRoutine = StartCoroutine(InvincibilityCoroutine());
        }

        private IEnumerator InvincibilityCoroutine()
        {
            IsInvincible = true;
            var elapsed = 0f;
            var blinkOn = true;

            while (elapsed < invincibilityDuration)
            {
                if (spriteRenderer != null)
                    spriteRenderer.enabled = blinkOn;
                blinkOn = !blinkOn;
                yield return new WaitForSeconds(blinkInterval);
                elapsed += blinkInterval;
            }

            if (spriteRenderer != null)
                spriteRenderer.enabled = true;
            IsInvincible = false;
            invincibilityRoutine = null;
        }
    }
}
