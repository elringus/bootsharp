namespace Backend;

// In the domain assembly we outline the contract of a prime computer service.
// The implementation goes to other assembly (Backend.Prime), so that
// domain is not coupled with the details.

public interface IPrimeBackend
{
    void StartComputing ();
    void StopComputing ();
    bool IsComputing ();
}
