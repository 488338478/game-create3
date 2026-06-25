using System.Collections;
using UnityEngine;

namespace GameCreate3
{
    public sealed class StunTriggerZone : MonoBehaviour
    {
        [Header("Stun")]
        [SerializeField] private string stunTrigger = "Stun";
        [SerializeField] private float stunDuration = 1f;
        [SerializeField] private LayerMask targetLayers = ~0;

        [Header("Audio")]
        [SerializeField] private AudioClip hitSfxClip;

        private Coroutine activeRoutine;

        private void OnCollisionEnter2D(Collision2D collision)
        {
            if (activeRoutine != null) return;
            if ((targetLayers & (1 << collision.gameObject.layer)) == 0) return;
            if (!collision.collider.TryGetComponent<SideScrollCharacterControllerBase>(out var player)) return;

            if (hitSfxClip != null)
                Core.GameAudioService.Instance?.PlaySFX(hitSfxClip);

            activeRoutine = StartCoroutine(StunRoutine(player));
        }

        private IEnumerator StunRoutine(SideScrollCharacterControllerBase player)
        {
            var body = player.GetComponent<Rigidbody2D>();
            var previousInputEnabled = player.InputEnabled;

            player.SetInputEnabled(false);
            if (body != null)
            {
                body.velocity = Vector2.zero;
                body.angularVelocity = 0f;
            }

            var animator = player.GetComponentInChildren<Animator>(true);
            if (animator != null && !string.IsNullOrEmpty(stunTrigger))
                animator.SetTrigger(stunTrigger);

            yield return new WaitForSeconds(stunDuration);

            player.SetInputEnabled(previousInputEnabled);
            activeRoutine = null;
        }
    }
}
