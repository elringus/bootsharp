using System;

namespace Test.Library;

public interface IBidirectional
{
    event Action<IBidirectional>? OnBiChanged;

    IBidirectional Bi { get; set; }

    IBidirectional EchoBi (IBidirectional bi);
}
