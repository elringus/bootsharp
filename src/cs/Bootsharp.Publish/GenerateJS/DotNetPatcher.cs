using System.Text;
using System.Text.RegularExpressions;
using static System.Text.RegularExpressions.RegexOptions;

namespace Bootsharp.Publish;

internal sealed class DotNetPatcher (string buildDir)
{
    private readonly string dotnet = Path.Combine(buildDir, "dotnet.js");
    private readonly string runtime = Path.Combine(buildDir, "dotnet.runtime.js");
    private readonly string native = Path.Combine(buildDir, "dotnet.native.js");

    public void Patch ()
    {
        RemoveMaps();
        RemoveWasmNag();
        PacifyBundlers();
        QualifyImports();
    }

    private void RemoveMaps ()
    {
        // Microsoft bundles .NET JavaScript sources pre-minified/uglified with source maps
        // referencing upstream sources we don't publish with the package.
        Rewrite(new(@"^\s*//# sourceMappingURL=.*?\.map\s*$\r?\n?", CultureInvariant | Multiline),
            "", dotnet, runtime, native);
    }

    private void RemoveWasmNag ()
    {
        // Removes "WebAssembly resource does not have the expected content type..." warning.
        Rewrite(new(@"\w+\(['""]WebAssembly resource does not have[^)]+\)", CultureInvariant),
            "true", dotnet);
    }

    private void PacifyBundlers ()
    {
        // Neutralizes the bundler-offending code in the .NET and Emscripten's generated ES modules.
        // We handle the imports and WASM loading ourselves, so their hacks are a dead code.
        Rewrite(new(@"(import|new URL)\(", CultureInvariant),
            "$1(/*@vite-ignore*//*webpackIgnore:true*/", dotnet, runtime, native);
        Rewrite(new(@"import\.meta\.url", CultureInvariant),
            "\"file:///c:/x.js\"", dotnet, runtime, native);
    }

    private void QualifyImports ()
    {
        // Deno requires 'node:' prefix on Node built-ins.
        Rewrite(new(@"import\(([^""')]*)['""](?!node:)(url|fs|process|module|path|crypto)['""]\)", CultureInvariant),
            "import($1\"node:$2\")", dotnet, runtime, native);
    }

    private static void Rewrite (Regex pattern, string with, params ReadOnlySpan<string> paths)
    {
        foreach (var path in paths)
            File.WriteAllText(path, pattern.Replace(File.ReadAllText(path, Encoding.UTF8), with), Encoding.UTF8);
    }
}
