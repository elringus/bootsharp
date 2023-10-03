using System.Text;

namespace Bootsharp.Builder;

internal sealed class ModulePatcher(string buildDirectory, bool threading, bool embed)
{
    private readonly string dotnet = Path.Combine(buildDirectory, "dotnet.js");
    private readonly string runtime = Path.Combine(buildDirectory, "dotnet.runtime.js");
    private readonly string native = Path.Combine(buildDirectory, "dotnet.native.js");
    private readonly string runtimeGen = Path.Combine(buildDirectory, "dotnet.runtime.g.js");
    private readonly string nativeGen = Path.Combine(buildDirectory, "dotnet.native.g.js");

    public void Patch ()
    {
        if (threading) PatchThreading();
        if (embed) new InternalPatcher(dotnet, runtime, native).Patch();
        CopyInternals();
    }

    private void PatchThreading ()
    {
        // Overprotective browser-only assert breaks unit testing:
        // https://github.com/dotnet/runtime/issues/92853.

        File.WriteAllText(dotnet, File.ReadAllText(dotnet, Encoding.UTF8)
            .Replace("&&Te(!1,\"This build of dotnet is multi-threaded, it doesn't support shell environments like V8 or NodeJS. See also https://aka.ms/dotnet-wasm-features\")", ""), Encoding.UTF8);
    }

    private void CopyInternals ()
    {
        const string undefined = "export default undefined;";
        File.WriteAllText(runtimeGen, threading ? undefined : File.ReadAllText(runtime, Encoding.UTF8), Encoding.UTF8);
        File.WriteAllText(nativeGen, threading ? undefined : File.ReadAllText(native, Encoding.UTF8), Encoding.UTF8);
    }
}
