namespace Bootsharp.Builder.Test;

public class PrepareBootsharpTest : BuildTaskTest
{
    protected string GeneratedSerializer => ReadProjectFile(serializerPath);

    private string serializerPath => $"{Project.Root}/SerializerContext.g.cs";

    public override void Execute () => CreateTask().Execute();

    private PrepareBootsharp CreateTask () => new() {
        InspectedDirectory = Project.Root,
        EntryAssemblyName = LastAddedAssemblyName ?? "System.Runtime.dll",
        SerializerFilePath = serializerPath,
        BuildEngine = Engine
    };
}
