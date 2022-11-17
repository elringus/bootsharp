namespace Backend.Domain;

public interface IBackend
{
    void StartStress ();
    void StopStress ();
    bool IsStressing ();
}
