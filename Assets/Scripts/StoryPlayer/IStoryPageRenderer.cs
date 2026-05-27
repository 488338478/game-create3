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
        void PrepareBackground(StoryPage page);

        bool RequestInput();
        void SkipCurrentAnimation();

        void SetSequenceFont(TMPro.TMP_FontAsset fontAsset);
        void SetPlaybackSpeed(float speed);
    }
}
