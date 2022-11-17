namespace Backend.Domain;

public interface IFrontend
{
    int GetStressPower ();
    void OnStressComplete (int time);
}
