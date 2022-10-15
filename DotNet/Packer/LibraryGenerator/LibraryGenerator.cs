using System;
using static Packer.TextUtilities;

namespace Packer;

internal class LibraryGenerator
{
    private readonly AssemblyInspector inspector;
    private readonly BindingsGenerator bindingsGenerator;
    private readonly string runtimeJS;
    private readonly string entryAssemblyName;

    public LibraryGenerator (NamespaceBuilder spaceBuilder, AssemblyInspector inspector,
        string runtimeJS, string entryAssemblyName)
    {
        this.inspector = inspector;
        this.runtimeJS = runtimeJS;
        this.entryAssemblyName = entryAssemblyName;
        bindingsGenerator = new(inspector, spaceBuilder);
    }

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
