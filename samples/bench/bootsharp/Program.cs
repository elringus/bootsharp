using Bootsharp;
using Bootsharp.Inject;
using Microsoft.Extensions.DependencyInjection;

[assembly: JSImport(typeof(IImport))]
[assembly: JSExport(typeof(IExport))]

new ServiceCollection()
    .AddBootsharp()
    .AddSingleton<IExport, Export>()
    .BuildServiceProvider()
    .RunBootsharp();

public struct Data
{
    public string Info;
    public bool Ok;
    public int Revision;
    public string[] Messages;
}

public interface IImport
{
    int GetNumber ();
    Data GetStruct ();
}

public interface IExport
{
    int EchoNumber ();
    Data EchoStruct ();
    int Fi (int n);
}

public class Export (IImport import) : IExport
{
    public int EchoNumber () => import.GetNumber();
    public Data EchoStruct () => import.GetStruct();
    public int Fi (int n) => n <= 1 ? n : Fi(n - 1) + Fi(n - 2);
}
