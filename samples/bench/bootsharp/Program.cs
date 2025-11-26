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
    public string Info { get; set; }
    public bool Ok { get; set; }
    public int Revision { get; set; }
    public string[] Messages { get; set; }
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
    public int Fi (int n) => F(n);
    // Due to heavy recursion, a significant degradation accumulates due to constant
    // dereferencing of the instance on each iteration, hence using the static version.
    private static int F (int n) => n <= 1 ? n : F(n - 1) + F(n - 2);
}
