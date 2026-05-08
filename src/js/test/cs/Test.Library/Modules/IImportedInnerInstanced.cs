using System;

namespace Test.Library;

public interface IImportedInnerInstanced
{
    event Action<int> OnCountChanged;

    int Count { get; set; }

    void Increment ();
}
