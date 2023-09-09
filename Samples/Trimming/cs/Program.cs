using System;
using Bootsharp;

[assembly: JSImport(typeof(IFrontend))]
[assembly: JSExport(typeof(IBackend))]

_ = new Backend.JSBackend(new NetBackend());
var info = Frontend.JSFrontend.GetInfo();
Frontend.JSFrontend.Log($"Frontend: {info.Environment}");

public record Info(string Environment);

public interface IFrontend
{
    void Log (string message);
    Info GetInfo ();
}

public interface IBackend
{
    Info GetInfo ();
}

internal class NetBackend : IBackend
{
    public Info GetInfo () => new($".NET {Environment.Version}");
}
