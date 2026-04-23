using System;

namespace Backend;

// In the domain assembly we outline the contract of a computer service.
// The specific implementation is in other assembly, so that
// domain is not coupled with the details.

public interface IComputer
{
    event Action<bool> OnComputing;
    event Action<long> OnComplete;
    void StartComputing ();
    void StopComputing ();
    bool IsComputing ();
}
