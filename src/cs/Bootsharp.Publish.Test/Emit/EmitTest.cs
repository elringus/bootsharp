namespace Bootsharp.Publish.Test;

public class EmitTest : TaskTest
{
    protected BootsharpEmit Task { get; }
    protected string GeneratedExports => ReadProjectFile(exportsPath);
    protected string GeneratedImports => ReadProjectFile(importsPath);
    protected string GeneratedDependencies => ReadProjectFile(dependenciesPath);
    protected string GeneratedSerializer => ReadProjectFile(serializerPath);
    protected string GeneratedInterop => ReadProjectFile(interopPath);

    private string exportsPath => $"{Project.Root}/Exports.g.cs";
    private string importsPath => $"{Project.Root}/Imports.g.cs";
    private string dependenciesPath => $"{Project.Root}/Dependencies.g.cs";
    private string serializerPath => $"{Project.Root}/Serializer.g.cs";
    private string interopPath => $"{Project.Root}/Interop.g.cs";

    public EmitTest ()
    {
        Task = CreateTask();
    }

    public override void Execute ()
    {
        if (LastAddedAssemblyName is not null)
            Task.EntryAssemblyName = LastAddedAssemblyName;
        Task.Execute();
    }

    private BootsharpEmit CreateTask () => new() {
        InspectedDirectory = Project.Root,
        EntryAssemblyName = "System.Runtime.dll",
        ExportsFilePath = exportsPath,
        ImportsFilePath = importsPath,
        DependenciesFilePath = dependenciesPath,
        SerializerFilePath = serializerPath,
        InteropFilePath = interopPath,
        BuildEngine = Engine
    };
}
