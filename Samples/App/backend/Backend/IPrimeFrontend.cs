namespace Backend;

// In the domain assembly we outline the contract of a prime computer UI.
// The implementation goes to frontend, so that neither domain, nor
// C# backend in general are coupled with the details.

public interface IPrimeFrontend
{
    int GetComplexity ();
    void NotifyComplete (int time);
}
