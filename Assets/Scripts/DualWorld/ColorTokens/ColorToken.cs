using UnityEngine;

namespace GameCreate3.DualWorld
{
    public readonly struct ColorToken
    {
        public ColorToken(string id, Color displayColor, string styleTag, bool unlockedByDream)
        {
            Id = id;
            DisplayColor = displayColor;
            StyleTag = styleTag;
            UnlockedByDream = unlockedByDream;
        }

        public string Id { get; }
        public Color DisplayColor { get; }
        public string StyleTag { get; }
        public bool UnlockedByDream { get; }
    }
}
