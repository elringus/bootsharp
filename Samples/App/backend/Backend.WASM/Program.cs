using Backend;
using DotNetJS;

[assembly: JSExport(new[] { typeof(IBackend) })]
[assembly: JSImport(new[] { typeof(IFrontend) }, namePattern: "Notify(.+)", nameReplacement: "On$1")]

namespace Backend.WASM;

public static class Program
{
    public static void Main ()
    {
        _ = new JSBackend(new Backend(new Frontend.JSFrontend()));
    }
}
