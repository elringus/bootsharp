namespace Backend;

public interface IFrontend
{
    int GetStressPower ();
    void NotifyStressComplete (int time);
}
