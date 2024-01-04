namespace Bootsharp.Publish.Test;

public class EmitTest : TaskTest
{
    protected string GeneratedInteropExports => ReadProjectFile(interopExportsPath);
    protected string GeneratedInteropImports => ReadProjectFile(interopImportsPath);
    protected string GeneratedSerializer => ReadProjectFile(serializerPath);

    private string interopExportsPath => $"{Project.Root}/InteropExports.g.cs";
    private string interopImportsPath => $"{Project.Root}/InteropImports.g.cs";
    private string serializerPath => $"{Project.Root}/SerializerContext.g.cs";

    public override void Execute () => CreateTask().Execute();

    private BootsharpEmit CreateTask () => new() {
        InspectedDirectory = Project.Root,
        EntryAssemblyName = LastAddedAssemblyName ?? "System.Runtime.dll",
        ExportsFilePath = interopExportsPath,
        ImportsFilePath = interopImportsPath,
        SerializerFilePath = serializerPath,
        BuildEngine = Engine
    };
}
