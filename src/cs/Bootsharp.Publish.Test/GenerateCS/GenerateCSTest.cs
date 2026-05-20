namespace Bootsharp.Publish.Test;

public class GenerateCSTest : TaskTest
{
    protected GenerateCS Task { get; }
    protected string GeneratedSerializer => ReadProjectFile(serializerPath);
    protected string GeneratedInstances => ReadProjectFile(instancesPath);
    protected string GeneratedModules => ReadProjectFile(modulesPath);
    protected string GeneratedInterop => ReadProjectFile(interopPath);
    protected string GeneratedImportsLibrary => ReadProjectFile(importsLibraryPath);

    private string serializerPath => $"{Project.Root}/Serializer.g.cs";
    private string instancesPath => $"{Project.Root}/Instances.g.cs";
    private string modulesPath => $"{Project.Root}/Modules.g.cs";
    private string interopPath => $"{Project.Root}/Interop.g.cs";
    private string importsLibraryPath => $"{Project.Root}/imports.g.js";

    public GenerateCSTest ()
    {
        Task = CreateTask();
    }

    public override void Execute ()
    {
        if (LastAddedAssemblyName is not null)
            Task.EntryAssemblyName = LastAddedAssemblyName;
        Task.Execute();
    }

    private GenerateCS CreateTask () => new() {
        InspectedDirectory = Project.Root,
        EntryAssemblyName = "System.Runtime.dll",
        SerializerFilePath = serializerPath,
        InstancesFilePath = instancesPath,
        ModulesFilePath = modulesPath,
        InteropFilePath = interopPath,
        ImportsLibraryFilePath = importsLibraryPath,
        BuildEngine = Engine
    };
}
