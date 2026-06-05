using System;

namespace Test.Library;

public interface IBidirectional
{
    event Action<IBidirectional?>? OnBiChanged;

    IBidirectional? Bi { get; set; }
    Event<SpecialBiHandler> OnSpecial { get; }

    IBidirectional? EchoBi (IBidirectional? bi);
}
