using Backend;
using DotNetJS;

[assembly: JSExport(new[] { typeof(IBackend) })]
[assembly: JSImport(new[] { typeof(IFrontend) })]

namespace Backend.WASM;

public static class Program
{
    public static void Main ()
    {
        _ = new JSBackend(new Backend(new JSFrontend()));
    }
}
