namespace Bootsharp.Builder;

internal sealed class ExportGenerator
{
    public string Generate (AssemblyInspector inspector)
    {
        return new ExportTemplate().Build();
    }
}
