namespace Backend;

// In the domain assembly we outline the contract of the computer UI.
// The implementation goes to frontend, so that neither domain, nor
// C# backend in general are coupled with the details.

public interface IComputerUI
{
    int GetComplexity ();
    void NotifyComplete (int time);
}
