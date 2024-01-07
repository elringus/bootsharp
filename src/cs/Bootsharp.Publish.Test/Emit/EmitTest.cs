namespace Bootsharp.Publish.Test;

public class EmitTest : TaskTest
{
    protected string GeneratedExports => ReadProjectFile(exportsPath);
    protected string GeneratedImports => ReadProjectFile(importsPath);
    protected string GeneratedInterceptors => ReadProjectFile(interceptorsPath);
    protected string GeneratedDependencies => ReadProjectFile(dependenciesPath);
    protected string GeneratedSerializer => ReadProjectFile(serializerPath);
    protected string GeneratedInterop => ReadProjectFile(interopPath);

    private string exportsPath => $"{Project.Root}/Exports.g.cs";
    private string importsPath => $"{Project.Root}/Imports.g.cs";
    private string interceptorsPath => $"{Project.Root}/Interceptors.g.cs";
    private string dependenciesPath => $"{Project.Root}/Dependencies.g.cs";
    private string serializerPath => $"{Project.Root}/SerializerContext.g.cs";
    private string interopPath => $"{Project.Root}/Interop.g.cs";

    public override void Execute () => CreateTask().Execute();

    private BootsharpEmit CreateTask () => new() {
        InspectedDirectory = Project.Root,
        EntryAssemblyName = LastAddedAssemblyName ?? "System.Runtime.dll",
        ExportsFilePath = exportsPath,
        ImportsFilePath = importsPath,
        InterceptorsFilePath = interceptorsPath,
        DependenciesFilePath = dependenciesPath,
        SerializerFilePath = serializerPath,
        InteropFilePath = interceptorsPath,
        BuildEngine = Engine
    };
}
