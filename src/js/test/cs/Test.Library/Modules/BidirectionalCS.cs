using System;

namespace Test.Library;

public class BidirectionalCS : IBidirectional
{
    public event Action<IBidirectional?>? OnBiChanged;
    public Event<SpecialBiHandler> OnSpecial { get; } = new();

    public IBidirectional? Bi { get; set => NotifyChanged(field = value); } = null!;

    public IBidirectional? EchoBi (IBidirectional? bi) => bi;

    private void NotifyChanged (IBidirectional? bi)
    {
        OnBiChanged?.Invoke(bi);
        OnSpecial.Broadcast(bi);
    }
}
