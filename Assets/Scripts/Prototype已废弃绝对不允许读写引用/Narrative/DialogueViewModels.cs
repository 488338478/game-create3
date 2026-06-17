using System;
using System.Collections.Generic;

namespace GameCreate3
{
    public sealed class DialogueViewModel
    {
        public string Speaker { get; }
        public string Body { get; }
        public IReadOnlyList<DialogueChoiceViewModel> Choices { get; }

        public DialogueViewModel(string speaker, string body, IReadOnlyList<DialogueChoiceViewModel> choices)
        {
            Speaker = speaker;
            Body = body;
            Choices = choices ?? Array.Empty<DialogueChoiceViewModel>();
        }
    }

    public sealed class DialogueChoiceViewModel
    {
        public int Index { get; }
        public string Text { get; }

        public DialogueChoiceViewModel(int index, string text)
        {
            Index = index;
            Text = text;
        }
    }
}
