namespace Bootsharp.Builder;

internal sealed class SerializerGenerator
{
    public string Generate (AssemblyInspector inspector)
    {
        var types = inspector.Types.Select(t => $"global::{t.FullName}");
        var attrs = types.Select(BuildAttribute).Append(BuildAttribute("global::System.String"));
        return
            $$"""
              using System.Text.Json;
              using System.Text.Json.Serialization;

              namespace Bootsharp;

              {{JoinLines(attrs, 0)}}
              internal partial class SerializerContext : JsonSerializerContext
              {
                  [System.Runtime.CompilerServices.ModuleInitializer]
                  internal static void InjectTypeInfoResolver ()
                  {
                      Serializer.Options.TypeInfoResolver = SerializerContext.Default;
                  }
              }
              """;
    }

    private string BuildAttribute (string type) =>
        $"[JsonSerializable(typeof({type}), TypeInfoPropertyName = \"{BuildHint(type)}\")]";

    private string BuildHint (string type) => type[8..].Replace('.', '_').Replace('+', '_');
}
