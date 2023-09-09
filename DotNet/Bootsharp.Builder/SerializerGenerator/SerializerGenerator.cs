namespace Bootsharp.Builder;

internal sealed class SerializerGenerator
{
    public string Generate (AssemblyInspector inspector)
    {
        var types = inspector.Types.Select(BuildType);
        return new SerializerTemplate(types).Build();
    }

    private string BuildType (Type type) => $"global::{type.FullName}";
}
