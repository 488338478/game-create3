using System.Collections.Generic;
using UnityEngine;

namespace GameCreate3.StoryPlayer
{
    public static class StoryTestDataGenerator
    {
        public static StorySequence CreateTestSequence()
        {
            var sequence = ScriptableObject.CreateInstance<StorySequence>();

            // Use reflection to set private fields
            SetPrivateField(sequence, "sequenceId", "TestSequence_001");
            SetPrivateField(sequence, "defaultPlaybackMode", StoryPlaybackMode.AutoAdvance);
            SetPrivateField(sequence, "allowSkip", true);
            SetPrivateField(sequence, "autoAdvanceDelay", 1.5f);
            SetPrivateField(sequence, "endCallbackType", StoryEndCallbackType.None);

            var pages = new List<StoryPage>();

            // Page 1 - Introduction
            var page1 = CreatePage(
                "page_001",
                StoryPageType.Text,
                "Chapter 1: Start",
                new List<StoryTextBlock>
                {
                    new StoryTextBlock
                    {
                        Speaker = "Narrator",
                        Content = "[Page 1: Auto Advance]\nThis page advances automatically after 1.5 seconds.\nNo input needed.",
                        DisplayMode = TextDisplayMode.Typewriter,
                        TypewriterSpeed = 0.05f
                    }
                },
                1.5f,
                false,
                StoryTransitionType.Fade,
                StoryTransitionType.Fade,
                0.5f
            );
            pages.Add(page1);

            // Page 2 - Click To Advance (manual input)
            var page2 = CreatePage(
                "page_002",
                StoryPageType.Text,
                "Manual Input Page",
                new List<StoryTextBlock>
                {
                    new StoryTextBlock
                    {
                        Speaker = "System",
                        Content = "[Page 2: Manual Test]\n- Click: advance\n- Click during typewriter: reveal full text\n- Hold ~1.5s: skip entire sequence",
                        DisplayMode = TextDisplayMode.Typewriter,
                        TypewriterSpeed = 0.04f
                    }
                },
                -1f,
                true,
                StoryTransitionType.SlideLeft,
                StoryTransitionType.SlideRight,
                0.8f
            );
            pages.Add(page2);

            // Page 3 - Event test
            var page3Events = new List<StoryPageEvent>
            {
                new StoryPageEvent
                {
                    EventType = StoryEventType.PlaySound,
                    TriggerTime = 0.5f,
                    EventData = "test_sound|0.8"
                },
                new StoryPageEvent
                {
                    EventType = StoryEventType.SetVariable,
                    TriggerTime = 1.0f,
                    EventData = "StoryTestPassed=true"
                }
            };

            var page3 = CreatePage(
                "page_003",
                StoryPageType.Mixed,
                "Event Trigger Test",
                new List<StoryTextBlock>
                {
                    new StoryTextBlock
                    {
                        Speaker = "Narrator",
                        Content = "[Page 3: Event Test]\nAutomatically triggers:\n1) PlaySound(test_sound)\n2) SetVariable(StoryTestPassed=true)",
                        DisplayMode = TextDisplayMode.Typewriter,
                        TypewriterSpeed = 0.04f
                    }
                },
                2.0f,
                false,
                StoryTransitionType.Fade,
                StoryTransitionType.Fade,
                0.5f,
                page3Events
            );
            pages.Add(page3);

            // Page 4 - Final manual page for skip/complete verification
            var page4 = CreatePage(
                "page_004",
                StoryPageType.Text,
                "Final Verification",
                new List<StoryTextBlock>
                {
                    new StoryTextBlock
                    {
                        Speaker = "System",
                        Content = "[Page 4: Final Check]\nClick to complete normally.\nPress Esc or hold to trigger Skip.",
                        DisplayMode = TextDisplayMode.Typewriter,
                        TypewriterSpeed = 0.03f
                    }
                },
                -1f,
                true,
                StoryTransitionType.Fade,
                StoryTransitionType.Fade,
                0.5f
            );
            pages.Add(page4);

            SetPrivateField(sequence, "pages", pages);

            return sequence;
        }

        private static StoryPage CreatePage(
            string pageId,
            StoryPageType pageType,
            string description,
            List<StoryTextBlock> textBlocks,
            float displayDuration,
            bool waitForInput,
            StoryTransitionType transitionIn,
            StoryTransitionType transitionOut,
            float transitionDuration,
            List<StoryPageEvent> events = null)
        {
            var page = new StoryPage();

            // Use reflection to set fields
            SetPrivateField(page, "pageId", pageId);
            SetPrivateField(page, "pageType", pageType);
            SetPrivateField(page, "textBlocks", textBlocks);
            SetPrivateField(page, "displayDuration", displayDuration);
            SetPrivateField(page, "waitForInput", waitForInput);
            SetPrivateField(page, "transitionIn", transitionIn);
            SetPrivateField(page, "transitionOut", transitionOut);
            SetPrivateField(page, "transitionDuration", transitionDuration);
            SetPrivateField(page, "pageEvents", events ?? new List<StoryPageEvent>());
            SetPrivateField(page, "elements", new List<StoryElement>());

            return page;
        }

        private static void SetPrivateField(object obj, string fieldName, object value)
        {
            var field = obj.GetType().GetField(fieldName, System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (field != null)
            {
                field.SetValue(obj, value);
            }
            else
            {
                // Try property with backing field
                var property = obj.GetType().GetProperty(fieldName);
                if (property != null && property.CanWrite)
                {
                    property.SetValue(obj, value);
                }
            }
        }
    }
}
