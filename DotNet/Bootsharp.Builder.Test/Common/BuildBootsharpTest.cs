namespace Bootsharp.Builder.Test;

public class BuildBootsharpTest : BuildTaskTest
{
    protected BuildBootsharp Task { get; }
    protected byte[] MockWasmBinary { get; } = "MockWasmContent"u8.ToArray();
    protected string GeneratedBindings => ReadProjectFile("bindings.g.js");
    protected string GeneratedDeclarations => ReadProjectFile("bindings.g.d.ts");
    protected string GeneratedResources => ReadProjectFile("resources.g.js");

    public BuildBootsharpTest ()
    {
        Task = CreateTask();
        Project.WriteFile("dotnet.native.wasm", MockWasmBinary);
    }

    public override void Execute ()
    {
        if (Project.Assemblies.Count > 0)
            Task.EntryAssemblyName = Project.Assemblies.Last().Name;
        Task.Execute();
    }

    private BuildBootsharp CreateTask () => new() {
        BuildDirectory = Project.Root,
        InspectedDirectory = Project.Root,
        EntryAssemblyName = "System.Runtime.dll",
        BuildEngine = Engine,
        EmbedBinaries = false
    };
}
