namespace Backend.Prime;

// Contract of the prime computer user interface.
// The implementation goes to the frontend,
// so that backend is not coupled with the details.

public interface IPrimeUI
{
    Options GetOptions ();
    // Imported methods starting with "Notify" will automatically
    // be converted to JavaScript events and renamed to "On...".
    // This can be configured with "JSImport.EventPattern" and
    // "JSImport.EventReplacement" attribute parameters.
    void NotifyComputing (bool computing);
    void NotifyComplete (long time);
}
