namespace Bootsharp.Publish.Test;

public class EmitTest : TaskTest
{
    protected BootsharpEmit Task { get; }
    protected string GeneratedSerializer => ReadProjectFile(serializerPath);
    protected string GeneratedInstances => ReadProjectFile(instancesPath);
    protected string GeneratedModules => ReadProjectFile(modulesPath);
    protected string GeneratedInterop => ReadProjectFile(interopPath);

    private string serializerPath => $"{Project.Root}/Serializer.g.cs";
    private string instancesPath => $"{Project.Root}/Instances.g.cs";
    private string modulesPath => $"{Project.Root}/Modules.g.cs";
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
        SerializerFilePath = serializerPath,
        InstancesFilePath = instancesPath,
        ModulesFilePath = modulesPath,
        InteropFilePath = interopPath,
        BuildEngine = Engine
    };
}
