using System.Text;
using System.Text.RegularExpressions;

namespace Bootsharp.Publish;

internal sealed class ModulePatcher (string buildDir)
{
    private readonly string dotnet = Path.Combine(buildDir, "dotnet.js");
    private readonly string runtime = Path.Combine(buildDir, "dotnet.runtime.js");
    private readonly string native = Path.Combine(buildDir, "dotnet.native.js");

    public void Patch ()
    {
        RemoveMaps();
        RemoveWasmNag();
    }

    private void RemoveMaps ()
    {
        // Microsoft bundles .NET JavaScript sources pre-minified/uglified with source maps
        // referencing upstream sources we don't publish with the package.
        var regex = new Regex(@"^\s*//# sourceMappingURL=.*?\.map\s*$\r?\n?",
            RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.Multiline);
        File.WriteAllText(dotnet, regex.Replace(File.ReadAllText(dotnet, Encoding.UTF8), ""), Encoding.UTF8);
        File.WriteAllText(runtime, regex.Replace(File.ReadAllText(runtime, Encoding.UTF8), ""), Encoding.UTF8);
        File.WriteAllText(native, regex.Replace(File.ReadAllText(native, Encoding.UTF8), ""), Encoding.UTF8);
    }

    private void RemoveWasmNag ()
    {
        // Removes "WebAssembly resource does not have the expected content type..." warning.
        File.WriteAllText(dotnet, new Regex("""(?:[$\w]+\.)*[$\w]+\(\s*(['"])WebAssembly resource does not have the expected content type \\?"application/wasm\\?", so falling back to slower ArrayBuffer instantiation\.\1\s*\)""",
                RegexOptions.Compiled | RegexOptions.CultureInvariant)
            .Replace(File.ReadAllText(dotnet, Encoding.UTF8), "true"), Encoding.UTF8);
    }
}
