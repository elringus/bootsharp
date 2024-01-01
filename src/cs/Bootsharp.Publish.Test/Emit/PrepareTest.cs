namespace Bootsharp.Publish.Test;

public class PrepareTest : TaskTest
{
    protected string GeneratedExports => ReadProjectFile(exportPath);
    protected string GeneratedImports => ReadProjectFile(importPath);
    protected string GeneratedSerializer => ReadProjectFile(serializerPath);

    private string exportPath => $"{Project.Root}/InteropExports.g.cs";
    private string importPath => $"{Project.Root}/InteropImports.g.cs";
    private string serializerPath => $"{Project.Root}/SerializerContext.g.cs";

    public override void Execute () => CreateTask().Execute();

    private BootsharpEmit CreateTask () => new() {
        InspectedDirectory = Project.Root,
        EntryAssemblyName = LastAddedAssemblyName ?? "System.Runtime.dll",
        ExportsFilePath = exportPath,
        ImportsFilePath = importPath,
        SerializerFilePath = serializerPath,
        BuildEngine = Engine
    };
}
