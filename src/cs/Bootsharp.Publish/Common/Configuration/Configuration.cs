using System.Runtime.Loader;

namespace Bootsharp.Publish;

internal sealed class Configuration (Preferences prefs, AssemblyLoadContext ctx) : IDisposable
{
    public Preferences Preferences => prefs;

    public void Dispose () => ctx.Unload();
}
