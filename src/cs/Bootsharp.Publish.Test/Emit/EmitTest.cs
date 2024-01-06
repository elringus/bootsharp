namespace Bootsharp.Publish.Test;

public class EmitTest : TaskTest
{
    protected string GeneratedInvokables => ReadProjectFile(invokablesPath);
    protected string GeneratedFunctions => ReadProjectFile(functionsPath);
    protected string GeneratedEvents => ReadProjectFile(eventsPath);
    protected string GeneratedExports => ReadProjectFile(exportsPath);
    protected string GeneratedImports => ReadProjectFile(importsPath);
    protected string GeneratedInteropExports => ReadProjectFile(interopExportsPath);
    protected string GeneratedInteropImports => ReadProjectFile(interopImportsPath);
    protected string GeneratedSerializer => ReadProjectFile(serializerPath);

    private string invokablesPath => $"{Project.Root}/Invokables.g.cs";
    private string functionsPath => $"{Project.Root}/Functions.g.cs";
    private string eventsPath => $"{Project.Root}/Events.g.cs";
    private string exportsPath => $"{Project.Root}/Exports.g.cs";
    private string importsPath => $"{Project.Root}/Imports.g.cs";
    private string interopExportsPath => $"{Project.Root}/InteropExports.g.cs";
    private string interopImportsPath => $"{Project.Root}/InteropImports.g.cs";
    private string serializerPath => $"{Project.Root}/SerializerContext.g.cs";

    public override void Execute () => CreateTask().Execute();

    private BootsharpEmit CreateTask () => new() {
        InspectedDirectory = Project.Root,
        EntryAssemblyName = LastAddedAssemblyName ?? "System.Runtime.dll",
        InvokablesFilePath = invokablesPath,
        FunctionsFilePath = functionsPath,
        EventsFilePath = eventsPath,
        ExportsFilePath = exportsPath,
        ImportsFilePath = importsPath,
        InteropExportsFilePath = interopExportsPath,
        InteropImportsFilePath = interopImportsPath,
        SerializerFilePath = serializerPath,
        BuildEngine = Engine
    };
}
