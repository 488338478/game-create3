using System;
using UnityEngine;

namespace GameCreate3.DualWorld
{
    [RequireComponent(typeof(Collider2D))]
    public sealed class DreamPushTarget : MonoBehaviour
    {
        [SerializeField] private float dwellSeconds = 0.5f;

        private float dwellTimer;
        private bool occupied;
        private bool completed;

        public event Action Completed;

        public void ResetTarget()
        {
            dwellTimer = 0f;
            occupied = false;
            completed = false;
        }

        private void OnTriggerStay2D(Collider2D other)
        {
            if (completed || other.GetComponent<DreamPushable>() == null)
            {
                return;
            }

            occupied = true;
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            if (other.GetComponent<DreamPushable>() != null)
            {
                occupied = false;
                dwellTimer = 0f;
            }
        }

        private void Update()
        {
            if (completed)
            {
                return;
            }

            if (occupied)
            {
                dwellTimer += Time.deltaTime;
                if (dwellTimer >= dwellSeconds)
                {
                    completed = true;
                    Completed?.Invoke();
                }
            }
        }
    }
}
