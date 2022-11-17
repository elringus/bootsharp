namespace Backend;

public interface IFrontend
{
    int GetStressPower ();
    void OnStressComplete (int time);
}
