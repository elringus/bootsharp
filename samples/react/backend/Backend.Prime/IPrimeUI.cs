namespace Backend.Prime;

// Contract of the prime computer user interface.
// The implementation goes to the frontend,
// so that backend is not coupled with the details.

public interface IPrimeUI
{
    Options GetOptions ();
}
