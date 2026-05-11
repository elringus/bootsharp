using System;

namespace Test.Library;

public class Bidirectional : IBidirectional
{
    public event Action<IBidirectional>? OnBiChanged;

    public IBidirectional Bi { get; set => OnBiChanged?.Invoke(field = value); } = null!;

    public IBidirectional EchoBi (IBidirectional bi) => bi;
}
