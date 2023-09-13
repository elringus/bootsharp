namespace Bootsharp.Builder.Test;

public class BuildTest : TaskTest
{
    protected BuildBootsharp Task { get; }
    protected byte[] MockWasmBinary { get; } = "MockWasmContent"u8.ToArray();
    protected string GeneratedBindings => ReadProjectFile("bindings.g.js");
    protected string GeneratedDeclarations => ReadProjectFile("bindings.g.d.ts");
    protected string GeneratedResources => ReadProjectFile("resources.g.js");

    public BuildTest ()
    {
        Task = CreateTask();
        Project.WriteFile("dotnet.native.wasm", MockWasmBinary);
    }

    public override void Execute ()
    {
        if (LastAddedAssemblyName is not null)
            Task.EntryAssemblyName = LastAddedAssemblyName;
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
