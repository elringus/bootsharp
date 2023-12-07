using System.Text;

namespace Bootsharp.Builder;

internal sealed class ModulePatcher (string buildDir, bool thread, bool embed, bool trim)
{
    private readonly string dotnet = Path.Combine(buildDir, "dotnet.js");
    private readonly string runtime = Path.Combine(buildDir, "dotnet.runtime.js");
    private readonly string native = Path.Combine(buildDir, "dotnet.native.js");
    private readonly string dotnetGen = Path.Combine(buildDir, "dotnet.g.js");
    private readonly string runtimeGen = Path.Combine(buildDir, "dotnet.runtime.g.js");
    private readonly string nativeGen = Path.Combine(buildDir, "dotnet.native.g.js");

    public void Patch ()
    {
        if (thread) PatchThreading();
        if (embed) new InternalPatcher(dotnet, runtime, native).Patch();
        if (trim) RemoveMaps();
        CopyInternals();
    }

    private void PatchThreading ()
    {
        // Overprotective browser-only assert breaks unit testing:
        // https://github.com/dotnet/runtime/issues/92853.

        File.WriteAllText(dotnet, File.ReadAllText(dotnet, Encoding.UTF8)
            .Replace("&&Te(!1,\"This build of dotnet is multi-threaded, it doesn't support shell environments like V8 or NodeJS. See also https://aka.ms/dotnet-wasm-features\")", ""), Encoding.UTF8);
    }

    private void RemoveMaps ()
    {
        // Microsoft bundles .NET JavaScript sources pre-minified/uglified with source maps.
        // When trimming enabled, we are not shipping the source maps, hence stripping the references here.
        // TODO: Raise an issue asking them to add an option to not uglify the sources.

        File.WriteAllText(dotnet, File.ReadAllText(dotnet, Encoding.UTF8)
            .Replace("//# sourceMappingURL=dotnet.js.map\n", ""), Encoding.UTF8);
        File.WriteAllText(runtime, File.ReadAllText(runtime, Encoding.UTF8)
            .Replace("//# sourceMappingURL=dotnet.runtime.js.map\n", ""), Encoding.UTF8);
    }

    private void CopyInternals ()
    {
        if (embed)
        {
            File.WriteAllText(dotnetGen, File.ReadAllText(dotnet, Encoding.UTF8), Encoding.UTF8);
            File.WriteAllText(runtimeGen, File.ReadAllText(runtime, Encoding.UTF8), Encoding.UTF8);
            File.WriteAllText(nativeGen, File.ReadAllText(native, Encoding.UTF8), Encoding.UTF8);
        }
        else
        {
            var mt = thread.ToString().ToLowerInvariant();
            var content = $"export const embedded = false;\nexport const mt = {mt};";
            File.WriteAllText(dotnetGen, content, Encoding.UTF8);
            File.WriteAllText(runtimeGen, content, Encoding.UTF8);
            File.WriteAllText(nativeGen, content, Encoding.UTF8);
        }
    }
}
