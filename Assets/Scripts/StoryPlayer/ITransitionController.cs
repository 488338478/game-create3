using System.Threading.Tasks;

namespace GameCreate3.StoryPlayer
{
    public interface ITransitionController
    {
        bool IsTransitioning { get; }

        Task PlayTransitionAsync(StoryTransitionType transitionType, float duration, bool isIn);
        void SkipCurrentTransition();
    }
}
