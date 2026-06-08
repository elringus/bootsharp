using System;

namespace Test.Library;

public interface IBidirectional
{
    public delegate void SpecialHandler (IBidirectional? bi);
    public sealed class SpecialEvent : Event<SpecialHandler>
    {
        public IBidirectional? Last { get; private set; }

        public void Broadcast (IBidirectional? bi)
        {
            Last = bi;
            foreach (var handler in Handlers)
                handler(bi);
        }

        public override void Replay (SpecialHandler handler)
        {
            if (Last is { } last)
                handler(last);
        }
    }

    event Action<IBidirectional?>? OnBiChanged;

    IBidirectional? Bi { get; set; }
    SpecialEvent OnSpecial { get; }

    IBidirectional? EchoBi (IBidirectional? bi);
}
