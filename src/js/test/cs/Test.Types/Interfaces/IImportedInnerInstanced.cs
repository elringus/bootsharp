using System;

namespace Test.Types;

public interface IImportedInnerInstanced
{
    event Action<int> OnCountChanged;

    int Count { get; set; }

    void Increment ();
}
