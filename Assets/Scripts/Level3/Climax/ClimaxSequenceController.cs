using System.Collections.Generic;
using GameCreate3.Core;
using UnityEngine;

namespace GameCreate3.Level3
{
    public sealed class ClimaxSequenceController : MonoBehaviour
    {
        [Header("Sequence")]
        [SerializeField] private int[] correctOrder = { 3, 1, 4, 2 };

        [Header("Threshold → Animal Mapping")]
        [SerializeField] private int[] thresholdRevealOrder = { 3, 1, 4, 2 };

        [Header("Animals (set in Inspector)")]
        [SerializeField] private ClimaxAnimalEntry[] animals;

        [Header("Path Blocks")]
        [SerializeField] private GameObject[] pathBlocks;
        [SerializeField] private float tempBlockDuration = 0.5f;

        [Header("Audio")]
        [SerializeField] private AudioClip sequenceCompleteClip;

        [Header("Exit Door")]
        [SerializeField] private GameObject exitDoor;

        private SideScrollWorkspaceBase workspace;
        private int currentStep;
        private int revealCount;
        private readonly HashSet<int> revealedAnimals = new HashSet<int>();
        private bool sequenceComplete;

        private void Awake()
        {
            workspace = GetComponentInParent<SideScrollWorkspaceBase>(true);
            // 仍需订阅：interact.animal.N 是动态事件 ID，无法在 Inspector 预配置
            if (workspace != null)
                workspace.WorkspaceEventRaised += OnWorkspaceEvent;
        }

        private void OnDestroy()
        {
            if (workspace != null)
                workspace.WorkspaceEventRaised -= OnWorkspaceEvent;
        }

        // --- WorkspaceEventRouter 调用的 public 入口 ---

        /// <summary>
        /// 粉丝达到任意阈值时调用（由 WorkspaceEventRouter 绑定 follower.2000~8000）。
        /// </summary>
        public void OnFollowerThreshold()
        {
            RevealNextAnimal();
        }

        // --- 动态事件仍由代码订阅 ---

        private void OnWorkspaceEvent(string eventId)
        {
            if (!eventId.StartsWith(Level3Events.InteractAnimalPrefix)) return;
            var numStr = eventId.Substring(Level3Events.InteractAnimalPrefix.Length);
            if (int.TryParse(numStr, out var animalIndex))
                TryInteract(animalIndex);
        }

        // --- 内部 ---

        private void RevealNextAnimal()
        {
            if (revealCount >= thresholdRevealOrder.Length) return;
            var animalIndex = thresholdRevealOrder[revealCount];
            revealCount++;

            if (revealedAnimals.Contains(animalIndex)) return;
            revealedAnimals.Add(animalIndex);

            var entry = GetAnimalEntry(animalIndex);
            if (entry != null && entry.animalObject != null)
                entry.animalObject.SetActive(true);

            workspace?.RaiseWorkspaceEvent(Level3Events.AnimalReveal(animalIndex));
        }

        private void TryInteract(int animalIndex)
        {
            if (sequenceComplete) return;
            if (!revealedAnimals.Contains(animalIndex)) return;

            if (animalIndex == correctOrder[currentStep])
            {
                SpawnPathBlock(currentStep, permanent: true);
                currentStep++;
                workspace?.RaiseWorkspaceEvent(Level3Events.SequenceCorrect);

                if (currentStep >= correctOrder.Length)
                    CompleteSequence();
            }
            else
            {
                workspace?.RaiseWorkspaceEvent(Level3Events.SequenceWrong);
                var wrongBlockIndex = System.Array.IndexOf(correctOrder, animalIndex);
                StartCoroutine(WrongSequenceReset(wrongBlockIndex));
                currentStep = 0;
            }
        }

        private void SpawnPathBlock(int stepIndex, bool permanent)
        {
            if (pathBlocks == null || stepIndex >= pathBlocks.Length) return;
            var block = pathBlocks[stepIndex];
            if (block == null) return;

            block.SetActive(true);
            if (!permanent)
                StartCoroutine(HideAfterDelay(block, tempBlockDuration));
        }

        private System.Collections.IEnumerator HideAfterDelay(GameObject obj, float delay)
        {
            yield return new WaitForSeconds(delay);
            if (obj != null)
                obj.SetActive(false);
        }

        private System.Collections.IEnumerator WrongSequenceReset(int wrongStep)
        {
            SpawnPathBlock(wrongStep, permanent: false);
            yield return new WaitForSeconds(tempBlockDuration);
            ResetAllPathBlocks();
        }

        private void ResetAllPathBlocks()
        {
            if (pathBlocks == null) return;
            foreach (var block in pathBlocks)
                if (block != null)
                    block.SetActive(false);
        }

        private void CompleteSequence()
        {
            sequenceComplete = true;
            if (sequenceCompleteClip != null && GameAudioService.Instance != null)
                GameAudioService.Instance.PlaySFX(sequenceCompleteClip);
            if (exitDoor != null)
                exitDoor.SetActive(true);
            workspace?.RaiseWorkspaceEvent(Level3Events.SequenceComplete);
        }

        private ClimaxAnimalEntry GetAnimalEntry(int index)
        {
            if (animals == null) return null;
            foreach (var entry in animals)
                if (entry.animalIndex == index)
                    return entry;
            return null;
        }
    }

    [System.Serializable]
    public sealed class ClimaxAnimalEntry
    {
        public int animalIndex;
        public GameObject animalObject;
    }
}
