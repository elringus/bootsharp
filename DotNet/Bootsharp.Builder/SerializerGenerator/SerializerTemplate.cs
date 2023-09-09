namespace Bootsharp.Builder;

internal sealed class SerializerTemplate(IEnumerable<string> types)
{
    public string Build () =>
        $$"""
          using System.Text.Json;
          using System.Text.Json.Serialization;

          namespace Bootsharp;

          {{BuildAttribute("string")}}
          {{JoinLines(types.Select(BuildAttribute), 0)}}
          internal partial class SerializerContext : JsonSerializerContext
          {
              [System.Runtime.CompilerServices.ModuleInitializer]
              internal static void InjectTypeInfoResolver ()
              {
                  Serializer.Options.TypeInfoResolver = SerializerContext.Default;
              }
          }
          """;

    private string BuildAttribute (string type) => $"[JsonSerializable(typeof({type}))]";
}
