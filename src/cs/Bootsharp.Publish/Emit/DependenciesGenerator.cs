namespace Bootsharp.Publish;

/// <summary>
/// Generates hints for DotNet to not trim specified dynamic dependencies, ie
/// members that are not statically accessed in the user source code.
/// </summary>
internal sealed class DependenciesGenerator
{
    public string Generate (AssemblyInspection inspection) => "";
}
