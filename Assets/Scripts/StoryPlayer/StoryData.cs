using System;
using System.Collections.Generic;
using UnityEngine;

namespace GameCreate3.StoryPlayer
{
    [CreateAssetMenu(fileName = "StorySequence_", menuName = "Game/StoryPlayer/Story Sequence")]
    public sealed class StorySequence : ScriptableObject
    {
        [SerializeField] private string sequenceId;
        [SerializeField] private List<StoryPage> pages = new List<StoryPage>();
        [SerializeField] private StoryPlaybackMode defaultPlaybackMode = StoryPlaybackMode.ClickToAdvance;
        [SerializeField] private bool allowSkip = true;
        [SerializeField] private float autoAdvanceDelay = 3f;
        [SerializeField] private StoryEndCallbackType endCallbackType = StoryEndCallbackType.None;
        [SerializeField] private string endCallbackParameter;

        public string SequenceId => sequenceId;
        public IReadOnlyList<StoryPage> Pages => pages;
        public StoryPlaybackMode DefaultPlaybackMode => defaultPlaybackMode;
        public bool AllowSkip => allowSkip;
        public float AutoAdvanceDelay => autoAdvanceDelay;
        public StoryEndCallbackType EndCallbackType => endCallbackType;
        public string EndCallbackParameter => endCallbackParameter;

        public bool TryGetPage(int index, out StoryPage page)
        {
            if (index >= 0 && index < pages.Count)
            {
                page = pages[index];
                return true;
            }
            page = null;
            return false;
        }

        public int PageCount => pages.Count;
    }

    public enum StoryEndCallbackType
    {
        None,
        EnterLevel,
        EnterSideScroller,
        EnterMainMenu,
        EnterDialogue,
        TriggerEvent
    }

    public enum StoryPlaybackMode
    {
        ClickToAdvance,
        AutoAdvance
    }

    [Serializable]
    public sealed class StoryPage
    {
        [SerializeField] private string pageId;
        [SerializeField] private StoryPageType pageType = StoryPageType.Mixed;
        [SerializeField] private Sprite backgroundImage;
        [SerializeField] private Sprite foregroundImage;
        [SerializeField] private List<StoryTextBlock> textBlocks = new List<StoryTextBlock>();
        [SerializeField] private float displayDuration = -1f;
        [SerializeField] private bool waitForInput = true;
        [SerializeField] private List<StoryElement> elements = new List<StoryElement>();
        [SerializeField] private StoryTransitionType transitionIn = StoryTransitionType.Fade;
        [SerializeField] private StoryTransitionType transitionOut = StoryTransitionType.Fade;
        [SerializeField] private float transitionDuration = 0.5f;
        [SerializeField] private List<StoryPageEvent> pageEvents = new List<StoryPageEvent>();
        [SerializeField] private StoryAudioConfig audioConfig;

        public string PageId => pageId;
        public StoryPageType PageType => pageType;
        public Sprite BackgroundImage => backgroundImage;
        public Sprite ForegroundImage => foregroundImage;
        public IReadOnlyList<StoryTextBlock> TextBlocks => textBlocks;
        public float DisplayDuration => displayDuration;
        public bool WaitForInput => waitForInput;
        public IReadOnlyList<StoryElement> Elements => elements;
        public StoryTransitionType TransitionIn => transitionIn;
        public StoryTransitionType TransitionOut => transitionOut;
        public float TransitionDuration => transitionDuration;
        public IReadOnlyList<StoryPageEvent> PageEvents => pageEvents;
        public StoryAudioConfig AudioConfig => audioConfig;
    }

    public enum StoryPageType
    {
        Static,
        Text,
        CG,
        Mixed
    }

    [Serializable]
    public sealed class StoryTextBlock
    {
        [SerializeField] private string textId;
        [SerializeField] private string speaker;
        [TextArea(3, 10)]
        [SerializeField] private string content;
        [SerializeField] private TextDisplayMode displayMode = TextDisplayMode.Typewriter;
        [SerializeField] private float typewriterSpeed = 0.05f;
        [SerializeField] private float delayBeforeShow;
        [SerializeField] private float duration = -1f;

        public string TextId { get => textId; set => textId = value; }
        public string Speaker { get => speaker; set => speaker = value; }
        public string Content { get => content; set => content = value; }
        public TextDisplayMode DisplayMode { get => displayMode; set => displayMode = value; }
        public float TypewriterSpeed { get => typewriterSpeed; set => typewriterSpeed = value; }
        public float DelayBeforeShow { get => delayBeforeShow; set => delayBeforeShow = value; }
        public float Duration { get => duration; set => duration = value; }
    }

    public enum TextDisplayMode
    {
        Instant,
        Typewriter,
        FadeIn
    }

    [Serializable]
    public sealed class StoryAudioConfig
    {
        [SerializeField] private AudioClip bgm;
        [SerializeField] private bool loopBgm = true;
        [SerializeField] private float bgmVolume = 1f;
        [SerializeField] private AudioClip voiceOver;
        [SerializeField] private List<StorySoundEffect> soundEffects = new List<StorySoundEffect>();

        public AudioClip Bgm => bgm;
        public bool LoopBgm => loopBgm;
        public float BgmVolume => bgmVolume;
        public AudioClip VoiceOver => voiceOver;
        public IReadOnlyList<StorySoundEffect> SoundEffects => soundEffects;
    }

    [Serializable]
    public sealed class StorySoundEffect
    {
        [SerializeField] private AudioClip clip;
        [SerializeField] private float triggerTime;
        [SerializeField] private float volume = 1f;

        public AudioClip Clip => clip;
        public float TriggerTime => triggerTime;
        public float Volume => volume;
    }

    [Serializable]
    public sealed class StoryElement
    {
        [SerializeField] private StoryElementType elementType;
        [SerializeField] private string elementId;
        [SerializeField] private Sprite image;
        [SerializeField] private string text;
        [SerializeField] private AudioClip audioClip;
        [SerializeField] private float delay;
        [SerializeField] private float duration = -1f;
        [SerializeField] private StoryAnimationType animationType = StoryAnimationType.None;
        [SerializeField] private float animationDuration = 0.5f;

        public StoryElementType ElementType => elementType;
        public string ElementId => elementId;
        public Sprite Image => image;
        public string Text => text;
        public AudioClip AudioClip => audioClip;
        public float Delay => delay;
        public float Duration => duration;
        public StoryAnimationType AnimationType => animationType;
        public float AnimationDuration => animationDuration;
    }

    [Serializable]
    public sealed class StoryPageEvent
    {
        [SerializeField] private StoryEventType eventType;
        [SerializeField] private float triggerTime;
        [SerializeField] private string eventData;

        public StoryEventType EventType { get => eventType; set => eventType = value; }
        public float TriggerTime { get => triggerTime; set => triggerTime = value; }
        public string EventData { get => eventData; set => eventData = value; }
    }

    public enum StoryElementType
    {
        Background,
        Character,
        DialogueText,
        NarrationText,
        Effect,
        Audio
    }

    public enum StoryTransitionType
    {
        None,
        Fade,
        SlideLeft,
        SlideRight,
        SlideUp,
        SlideDown,
        Scale,
        CrossFade
    }

    public enum StoryAnimationType
    {
        None,
        FadeIn,
        SlideIn,
        ScaleIn,
        Typewriter,
        Shake
    }

    public enum StoryEventType
    {
        PlaySound,
        PlayMusic,
        StopMusic,
        TriggerEffect,
        SetVariable,
        Branch,
        PostProcessEffect
    }
}
