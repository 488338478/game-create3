using System;

namespace GameCreate3.DualWorld
{
    public sealed class CrossWorldEventBus
    {
        public event Action<CrossWorldEvent> EventRaised;

        public void Raise(CrossWorldEvent evt)
        {
            EventRaised?.Invoke(evt);
        }

        public void Clear()
        {
            EventRaised = null;
        }
    }
}
