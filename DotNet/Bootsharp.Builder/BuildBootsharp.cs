using System.Text;
using Microsoft.Build.Framework;

namespace Bootsharp.Builder;

public sealed class BuildBootsharp : Microsoft.Build.Utilities.Task
{
    [Required] public required string BuildDirectory { get; set; }
    [Required] public required string InspectedDirectory { get; set; }
    [Required] public required string EntryAssemblyName { get; set; }
    [Required] public required bool EmbedBinaries { get; set; }

    public override bool Execute ()
    {
        var spaceBuilder = CreateNamespaceBuilder();
        using var inspector = InspectAssemblies(spaceBuilder);
        GenerateBindings(inspector, spaceBuilder);
        GenerateDeclarations(inspector, spaceBuilder);
        GenerateResources(inspector);
        PatchInternals();
        return true;
    }

    private NamespaceBuilder CreateNamespaceBuilder ()
    {
        var builder = new NamespaceBuilder();
        builder.CollectConverters(InspectedDirectory, EntryAssemblyName);
        return builder;
    }

    private AssemblyInspector InspectAssemblies (NamespaceBuilder spaceBuilder)
    {
        var inspector = new AssemblyInspector(spaceBuilder);
        inspector.InspectInDirectory(InspectedDirectory);
        inspector.Report(Log);
        return inspector;
    }

    private void GenerateBindings (AssemblyInspector inspector, NamespaceBuilder spaceBuilder)
    {
        var generator = new BindingGenerator(spaceBuilder);
        var content = generator.Generate(inspector);
        File.WriteAllText(Path.Combine(BuildDirectory, "bindings.g.js"), content);
    }

    private void GenerateDeclarations (AssemblyInspector inspector, NamespaceBuilder spaceBuilder)
    {
        var generator = new DeclarationGenerator(spaceBuilder);
        var content = generator.Generate(inspector);
        File.WriteAllText(Path.Combine(BuildDirectory, "bindings.g.d.ts"), content);
    }

    private void GenerateResources (AssemblyInspector inspector)
    {
        var generator = new ResourceGenerator(EntryAssemblyName, BuildDirectory, EmbedBinaries);
        var content = generator.Generate(inspector);
        File.WriteAllText(Path.Combine(BuildDirectory, "resources.g.js"), content);
    }

    private void PatchInternals ()
    {
        // Remove unnecessary environment-specific calls in .NET's internals,
        // that are offending bundlers. (https://github.com/dotnet/runtime/issues/91558)

        var dotnet = Path.Combine(BuildDirectory, "dotnet.js");
        File.WriteAllText(dotnet, File.ReadAllText(dotnet, Encoding.UTF8)
            .Replace("=import.meta.url", "=\"file:/\"")
            .Replace("import(", "import(/*@vite-ignore*//*webpackIgnore:true*/"), Encoding.UTF8);

        var native = Path.Combine(BuildDirectory, "dotnet.native.js");
        File.WriteAllText(native, File.ReadAllText(native, Encoding.UTF8)
            .Replace("var _scriptDir = import.meta.url", "var _scriptDir = \"file:/\"")
            .Replace("require('url').fileURLToPath(new URL('./', import.meta.url))", "\"./\"")
            .Replace("new URL('dotnet.native.wasm', import.meta.url).href", "\"file:/\"")
            .Replace("import(", "import(/*@vite-ignore*//*webpackIgnore:true*/"), Encoding.UTF8);

        var runtime = Path.Combine(BuildDirectory, "dotnet.runtime.js");
        File.WriteAllText(runtime, File.ReadAllText(runtime, Encoding.UTF8)
            .Replace("pt('WebAssembly resource does not have the expected content type \"application/wasm\", so falling back to slower ArrayBuffer instantiation.')", "true")
            .Replace("import(", "import(/*@vite-ignore*//*webpackIgnore:true*/"), Encoding.UTF8);
    }
}
