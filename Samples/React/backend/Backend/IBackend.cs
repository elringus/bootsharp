namespace Backend;

public interface IBackend
{
    void StartStress ();
    void StopStress ();
    bool IsStressing ();
}
