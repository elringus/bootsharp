using System.Text;
using System.Text.RegularExpressions;

namespace Bootsharp.Publish;

internal sealed class ModulePatcher (string buildDir, bool thread, bool embed)
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
        RemoveMaps();
        RemoveWasmNag();
        CopyInternals();
    }

    private void RemoveWasmNag ()
    {
        // Removes "WebAssembly resource does not have the expected content type..." warning.

        File.WriteAllText(dotnet, new Regex("""(?:[$\w]+\.)*[$\w]+\(\s*(['"])WebAssembly resource does not have the expected content type \\?"application/wasm\\?", so falling back to slower ArrayBuffer instantiation\.\1\s*\)""",
                RegexOptions.Compiled | RegexOptions.CultureInvariant)
            .Replace(File.ReadAllText(dotnet, Encoding.UTF8), "true"), Encoding.UTF8);
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
        // Microsoft bundles .NET JavaScript sources pre-minified/uglified with source maps
        // referencing upstream sources we don't publish with the package.
        // TODO: Raise an issue asking them to add an option to not uglify the sources.

        var regex = new Regex(@"^\s*//# sourceMappingURL=.*?\.map\s*$\r?\n?",
            RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.Multiline);
        File.WriteAllText(dotnet, regex.Replace(File.ReadAllText(dotnet, Encoding.UTF8), ""), Encoding.UTF8);
        File.WriteAllText(runtime, regex.Replace(File.ReadAllText(runtime, Encoding.UTF8), ""), Encoding.UTF8);
        File.WriteAllText(native, regex.Replace(File.ReadAllText(native, Encoding.UTF8), ""), Encoding.UTF8);
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
