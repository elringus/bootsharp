namespace Bootsharp.Builder;

internal sealed class ExportGenerator
{
    public static string Generate (AssemblyInspector inspector)
    {
        return new ExportTemplate().Build();
    }
}
