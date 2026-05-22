namespace Bootsharp.Publish;

/// <summary>
/// Generates import bindings for .NET's JavaScript interop infra.
/// </summary>
internal sealed class JSImportsGenerator
{
    public string Generate (JSModules mds) =>
        $$"""
          {{Fmt(mds.List.Select(EmitImport), 0)}}

          export function bindImports(runtime) {
              {{Fmt(mds.List.Select(EmitBinding))}}
          }
          """;

    private string EmitImport (JSModule md) =>
        $"""import * as {md.Alias} from "./modules/{md.Path}.g.mjs";""";

    private string EmitBinding (JSModule md) =>
        $"""runtime.setModuleImports("{md.Path}", {md.Alias});""";
}
