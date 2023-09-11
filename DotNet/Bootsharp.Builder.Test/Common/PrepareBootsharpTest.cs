namespace Bootsharp.Builder.Test;

public class PrepareBootsharpTest : BuildTaskTest
{
    protected string GeneratedExports => ReadProjectFile(exportPath);
    protected string GeneratedImports => ReadProjectFile(importPath);
    protected string GeneratedSerializer => ReadProjectFile(serializerPath);

    private string exportPath => $"{Project.Root}/InteropExports.g.cs";
    private string importPath => $"{Project.Root}/InteropImports.g.cs";
    private string serializerPath => $"{Project.Root}/SerializerContext.g.cs";

    public override void Execute () => CreateTask().Execute();

    private PrepareBootsharp CreateTask () => new() {
        InspectedDirectory = Project.Root,
        EntryAssemblyName = LastAddedAssemblyName ?? "System.Runtime.dll",
        InteropExportsFilePath = exportPath,
        InteropImportsFilePath = importPath,
        SerializerFilePath = serializerPath,
        BuildEngine = Engine
    };
}
