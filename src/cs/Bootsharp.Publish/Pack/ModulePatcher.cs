using System.Text;
using System.Text.RegularExpressions;
using static System.Text.RegularExpressions.RegexOptions;

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
        PacifyBundlers();
    }

    private void RemoveMaps ()
    {
        // Microsoft bundles .NET JavaScript sources pre-minified/uglified with source maps
        // referencing upstream sources we don't publish with the package.
        var regex = new Regex(@"^\s*//# sourceMappingURL=.*?\.map\s*$\r?\n?", Compiled | CultureInvariant | Multiline);
        File.WriteAllText(dotnet, regex.Replace(File.ReadAllText(dotnet, Encoding.UTF8), ""), Encoding.UTF8);
        File.WriteAllText(runtime, regex.Replace(File.ReadAllText(runtime, Encoding.UTF8), ""), Encoding.UTF8);
        File.WriteAllText(native, regex.Replace(File.ReadAllText(native, Encoding.UTF8), ""), Encoding.UTF8);
    }

    private void RemoveWasmNag ()
    {
        // Removes "WebAssembly resource does not have the expected content type..." warning.
        var regex = new Regex(@"\w+\(['""]WebAssembly resource does not have[^)]+\)", Compiled | CultureInvariant);
        File.WriteAllText(dotnet, regex.Replace(File.ReadAllText(dotnet, Encoding.UTF8), "true"), Encoding.UTF8);
    }

    private void PacifyBundlers ()
    {
        // Neutralizes the bundler-offending code in the .NET and Emscripten's generated ES modules.
        // We handle the imports and WASM loading ourselves, so their hacks are a dead code.
        var imp = new Regex(@"(?<![.$\w])import\((?:[^()]|\([^()]*\))*\)", Compiled | CultureInvariant);
        var req = new Regex(@"(?<![.$\w])require\((?:[^()]|\([^()]*\))*\)", Compiled | CultureInvariant);
        File.WriteAllText(dotnet, req.Replace(imp.Replace(File.ReadAllText(dotnet, Encoding.UTF8),
            "Promise.resolve()"), "null").Replace("import.meta.url", "\"file:/\""), Encoding.UTF8);
        File.WriteAllText(runtime, req.Replace(imp.Replace(File.ReadAllText(runtime, Encoding.UTF8),
            "Promise.resolve()"), "null").Replace("import.meta.url", "\"file:/\""), Encoding.UTF8);
        File.WriteAllText(native, req.Replace(imp.Replace(File.ReadAllText(native, Encoding.UTF8),
            "Promise.resolve()"), "null").Replace("import.meta.url", "\"file:/\""), Encoding.UTF8);
    }
}
