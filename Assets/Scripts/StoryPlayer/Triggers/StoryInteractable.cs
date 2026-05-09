using UnityEngine;

namespace GameCreate3.StoryPlayer
{
    /// <summary>
    /// 玩家走近 + 按交互键（默认 E）时播放剧情。继承 SideScroll 的交互基类，
    /// 自动接入工作区交互系统。
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public sealed class StoryInteractable : SideScrollInteractableBase
    {
        [SerializeField] private StorySequence sequence;
        [SerializeField] private bool oneShot = true;

        private bool fired;

        public override void Interact(GameObject interactor)
        {
            if (oneShot && fired) return;
            if (sequence == null) return;

            fired = true;
            StoryPlayerService.Play(sequence);
        }
    }
}
