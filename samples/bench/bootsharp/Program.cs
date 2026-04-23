using Bootsharp;
using Bootsharp.Inject;
using Microsoft.Extensions.DependencyInjection;

[assembly: Import(typeof(IImported))]
[assembly: Export(typeof(IExported))]

new ServiceCollection()
    .AddBootsharp()
    .AddSingleton<IExported, Exported>()
    .BuildServiceProvider()
    .RunBootsharp();

public struct Data
{
    public string Info { get; set; }
    public bool Ok { get; set; }
    public int Revision { get; set; }
    public string[] Messages { get; set; }
}

public interface IImported
{
    int GetNumber ();
    Data GetStruct ();
}

public interface IExported
{
    int EchoNumber ();
    Data EchoStruct ();
    int Fi (int n);
}

public class Exported (IImported imported) : IExported
{
    public int EchoNumber () => imported.GetNumber();
    public Data EchoStruct () => imported.GetStruct();
    public int Fi (int n) => F(n);
    // Due to heavy recursion, a significant degradation accumulates due to constant
    // dereferencing of the instance on each iteration, hence using the static version.
    private static int F (int n) => n <= 1 ? n : F(n - 1) + F(n - 2);
}
