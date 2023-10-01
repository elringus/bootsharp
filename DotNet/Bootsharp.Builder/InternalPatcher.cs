using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace Bootsharp.Builder;

[ExcludeFromCodeCoverage(Justification = "How to merge coverage from multiple OS?")]
internal sealed class InternalPatcher(string buildDirectory, bool multithreading)
{
    private readonly string url = Environment.OSVersion.Platform == PlatformID.Win32NT
        ? "\"file://dotnet.native.wasm\""
        : "\"file:///dotnet.native.wasm\"";

    public void Patch ()
    {
        // Remove unnecessary environment-specific calls in .NET's internals,
        // that are offending bundlers and breaking usage in restricted environments,
        // such as VS Code web extensions. (https://github.com/dotnet/runtime/issues/91558)

        if (multithreading)
        {
            PatchMultithreading();
            return;
        }

        PatchMain();
        PatchNative();
        PatchRuntime();
    }

    private void PatchMain ()
    {
        var dotnet = Path.Combine(buildDirectory, "dotnet.js");
        File.WriteAllText(dotnet, File.ReadAllText(dotnet, Encoding.UTF8)
            .Replace("import.meta.url", url)
            .Replace("import(", "import(/*@vite-ignore*//*webpackIgnore:true*/"), Encoding.UTF8);
    }

    private void PatchNative ()
    {
        var native = Path.Combine(buildDirectory, "dotnet.native.js");
        File.WriteAllText(native, File.ReadAllText(native, Encoding.UTF8)
            .Replace("var _scriptDir = import.meta.url", "var _scriptDir = \"file:/\"")
            .Replace("require('url').fileURLToPath(new URL('./', import.meta.url))", "\"./\"")
            .Replace("require(\"url\").fileURLToPath(new URL(\"./\",import.meta.url))", "\"./\"") // when aggressive trimming enabled
            .Replace("new URL('dotnet.native.wasm', import.meta.url).href", "\"file:/\"")
            .Replace("new URL(\"dotnet.native.wasm\",import.meta.url).href", "\"file:/\"") // when aggressive trimming enabled
            .Replace("import.meta.url", url)
            .Replace("import(", "import(/*@vite-ignore*//*webpackIgnore:true*/"), Encoding.UTF8);
    }

    private void PatchRuntime ()
    {
        var runtime = Path.Combine(buildDirectory, "dotnet.runtime.js");
        File.WriteAllText(runtime, File.ReadAllText(runtime, Encoding.UTF8)
            .Replace("pt('WebAssembly resource does not have the expected content type \"application/wasm\", so falling back to slower ArrayBuffer instantiation.')", "true")
            .Replace("import(", "import(/*@vite-ignore*//*webpackIgnore:true*/"), Encoding.UTF8);
    }

    private void PatchMultithreading ()
    {
        // Overprotective browser-only assert breaks unit testing:
        // https://github.com/dotnet/runtime/issues/92853.

        var dotnet = Path.Combine(buildDirectory, "dotnet.js");
        File.WriteAllText(dotnet, File.ReadAllText(dotnet, Encoding.UTF8)
            .Replace("&&Te(!1,\"This build of dotnet is multi-threaded, it doesn't support shell environments like V8 or NodeJS. See also https://aka.ms/dotnet-wasm-features\")", ""), Encoding.UTF8);
    }
}
