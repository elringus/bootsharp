namespace Backend;

// In the domain assembly we outline the contract of a computer service.
// The specific implementation is in other assembly, so that
// domain is not coupled with the details.

public interface IComputer
{
    void StartComputing ();
    void StopComputing ();
    bool IsComputing ();
}
