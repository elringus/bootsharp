namespace Bootsharp.Publish.Test;

public class EmitTest : TaskTest
{
    protected BootsharpEmit Task { get; }
    protected string GeneratedInterfaces => ReadProjectFile(interfacesPath);
    protected string GeneratedDependencies => ReadProjectFile(dependenciesPath);
    protected string GeneratedInterop => ReadProjectFile(interopPath);

    private string interfacesPath => $"{Project.Root}/Interfaces.g.cs";
    private string dependenciesPath => $"{Project.Root}/Dependencies.g.cs";
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
        InterfacesFilePath = interfacesPath,
        DependenciesFilePath = dependenciesPath,
        InteropFilePath = interopPath,
        BuildEngine = Engine
    };
}
