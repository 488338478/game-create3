using System;
using System.Threading.Tasks;

namespace GameCreate3.StoryPlayer
{
    public interface IStoryPageRenderer
    {
        bool IsReady { get; }
        bool IsRendering { get; }

        event Action OnRenderComplete;
        event Action OnInputRequested;

        void Initialize();
        void Cleanup();

        Task RenderPageAsync(StoryPage page);
        Task HidePageAsync(StoryPage page, StoryTransitionType transitionType, float duration);

        void RequestInput();
        void SkipCurrentAnimation();

        void SetPlaybackSpeed(float speed);
    }
}
