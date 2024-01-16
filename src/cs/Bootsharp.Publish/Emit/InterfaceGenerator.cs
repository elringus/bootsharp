namespace Bootsharp.Publish;

/// <summary>
/// Generates interop classes for interfaces specified under
/// <see cref="JSExportAttribute"/> and <see cref="JSImportAttribute"/>.
/// </summary>
internal sealed class InterfaceGenerator
{
    private readonly HashSet<string> exports = [];
    private readonly HashSet<string> imports = [];
    private readonly HashSet<string> registrations = [];

    public string Generate (AssemblyInspection inspection)
    {
        return
            $$"""
              #nullable enable
              #pragma warning disable

              namespace Bootsharp.Generated.Exports
              {
                  {{JoinLines(exports)}}
              }

              namespace Bootsharp.Generated.Imports
              {
                  {{JoinLines(imports)}}
              }

              namespace Bootsharp.Generated
              {
                  internal static class InterfaceRegistrations
                  {
                      [System.Runtime.CompilerServices.ModuleInitializer]
                      internal static void RegisterInterfaces ()
                      {
                          {{JoinLines(registrations, 3)}}
                      }
                  }
              }
              """;
    }

    private void AddExports () { }

    private void AddImports () { }
}
