using System;
using static Bootsharp.Packer.TextUtilities;

namespace Bootsharp.Packer;

internal sealed class LibraryGenerator(
    NamespaceBuilder spaceBuilder,
    AssemblyInspector inspector,
    string runtimeJS,
    string entryAssemblyName)
{
    private readonly BindingsGenerator bindingsGenerator = new(inspector, spaceBuilder);

    public string GenerateSideLoad (string wasmUri) =>
        GenerateLibrary(new BootUrisTemplate {
            WasmUri = wasmUri,
            Assemblies = inspector.Assemblies,
            EntryAssemblyUri = entryAssemblyName
        }.Build());

    public string GenerateEmbedded (byte[] wasmBytes) =>
        GenerateLibrary(new EmbedTemplate {
            RuntimeWasm = Convert.ToBase64String(wasmBytes),
            Assemblies = inspector.Assemblies,
            EntryAssemblyName = entryAssemblyName
        }.Build());

    private string GenerateLibrary (string initJS) => new LibraryTemplate {
        RuntimeJS = runtimeJS,
        InitJS = JoinLines(bindingsGenerator.Generate(), initJS)
    }.Build();
}
