namespace Bootsharp.Publish;

/// <summary>
/// Generates hints for .NET to not trim specified dynamic dependencies, ie
/// members that are not explicitly accessed in the user source code.
/// </summary>
internal sealed class DependencyGenerator (string entryAssembly)
{
    private readonly HashSet<string> added = [];

    public string Generate (SolutionInspection inspection)
    {
        AddGeneratedCommon();
        AddGeneratedInteropClasses(inspection);
        AddClassesWithInteropMethods(inspection);
        return
            $$"""
              using System.Diagnostics.CodeAnalysis;

              namespace Bootsharp.Generated;

              public static class Dependencies
              {
                  private const DynamicallyAccessedMemberTypes types =
                      DynamicallyAccessedMemberTypes.PublicMethods |
                      DynamicallyAccessedMemberTypes.NonPublicMethods |
                      DynamicallyAccessedMemberTypes.PublicFields |
                      DynamicallyAccessedMemberTypes.NonPublicFields |
                      DynamicallyAccessedMemberTypes.PublicNestedTypes |
                      DynamicallyAccessedMemberTypes.NonPublicNestedTypes |
                      DynamicallyAccessedMemberTypes.PublicProperties |
                      DynamicallyAccessedMemberTypes.NonPublicProperties |
                      DynamicallyAccessedMemberTypes.PublicEvents |
                      DynamicallyAccessedMemberTypes.NonPublicEvents |
                      DynamicallyAccessedMemberTypes.Interfaces;

                  [System.Runtime.CompilerServices.ModuleInitializer]
                  {{Fmt(added)}}
                  internal static void RegisterDynamicDependencies () { }
              }
              """;
    }

    private void AddGeneratedCommon ()
    {
        Add("Bootsharp.Generated.Dependencies", entryAssembly);
        Add("Bootsharp.Generated.Interop", entryAssembly);
    }

    private void AddGeneratedInteropClasses (SolutionInspection inspection)
    {
        foreach (var it in inspection.StaticInterfaces)
            Add(it.FullName, entryAssembly);
        foreach (var it in inspection.InstancedInterfaces)
            if (it.Interop == InteropKind.Import)
                Add(it.FullName, entryAssembly);
    }

    private void AddClassesWithInteropMethods (SolutionInspection inspection)
    {
        foreach (var member in inspection.StaticMembers)
            Add(member.Space, member.Assembly);
    }

    private void Add (string name, string assembly)
    {
        var asm = assembly.EndsWith(".dll", StringComparison.Ordinal) ? assembly[..^4] : assembly;
        added.Add($"""[DynamicDependency(types, "{name}", "{asm}")]""");
    }
}
