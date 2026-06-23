using TMPro;
using UnityEngine;

namespace GameCreate3.Level3
{
    public sealed class Level3HUD : MonoBehaviour
    {
        [Header("Text")]
        [SerializeField] private TextMeshProUGUI promptText;

        [Header("Phase Prompts")]
        [SerializeField] private string phase1Prompt = "躲避攻击！";
        [SerializeField] private string phase2Prompt = "按 [E] 键弹反";
        [SerializeField] private string phase3Prompt = "按顺序与动物互动：3 → 1 → 4 → 2";

        [Header("Hit Warning")]
        [SerializeField] private GameObject hitWarningFlash;
        [SerializeField] private float hitWarningDuration = 0.3f;

        [Header("Health Display")]
        [SerializeField] private GameObject[] healthIcons;

        private float hitWarningTimer;

        private void Start()
        {
            if (promptText != null)
                promptText.gameObject.SetActive(false);
        }

        private void Update()
        {
            if (hitWarningTimer > 0f)
            {
                hitWarningTimer -= Time.deltaTime;
                if (hitWarningTimer <= 0f && hitWarningFlash != null)
                    hitWarningFlash.SetActive(false);
            }
        }

        // --- WorkspaceEventRouter 调用的 public 入口 ---

        public void OnPhase1() => ShowPrompt(phase1Prompt);
        public void OnPhase2() => ShowPrompt(phase2Prompt);
        public void OnPhase3() => ShowPrompt(phase3Prompt);
        public void OnPlayerHit() => ShowHitWarning();
        public void OnLevelEnd() => HidePrompt();

        public void UpdateHealthIcons(int currentHits)
        {
            if (healthIcons == null) return;
            for (var i = 0; i < healthIcons.Length; i++)
                if (healthIcons[i] != null)
                    healthIcons[i].SetActive(i < currentHits);
        }

        // ---

        private void ShowPrompt(string text)
        {
            if (promptText != null)
            {
                promptText.text = text;
                promptText.gameObject.SetActive(true);
            }
        }

        private void HidePrompt()
        {
            if (promptText != null)
                promptText.gameObject.SetActive(false);
        }

        private void ShowHitWarning()
        {
            if (hitWarningFlash != null)
            {
                hitWarningFlash.SetActive(true);
                hitWarningTimer = hitWarningDuration;
            }
        }
    }
}
