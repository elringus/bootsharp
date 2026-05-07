using System;

namespace Test.Types;

public class ExportedInnerInstanced
{
    public event Action<int> OnCountChanged = delegate { };

    public int Count { get; set => OnCountChanged(field = value); }

    public void Increment () => Count++;
}
