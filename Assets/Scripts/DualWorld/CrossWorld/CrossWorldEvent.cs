namespace GameCreate3.DualWorld
{
    public enum CrossWorldEventType
    {
        DreamUnlocked,
        DreamCompleted,
        RealityCompleted,
        DreamWorldResolved,
        ExitReached
    }

    public readonly struct CrossWorldEvent
    {
        public CrossWorldEvent(CrossWorldEventType type, string subLevelId, object payload)
        {
            Type = type;
            SubLevelId = subLevelId;
            Payload = payload;
        }

        public CrossWorldEventType Type { get; }
        public string SubLevelId { get; }
        public object Payload { get; }
    }
}
