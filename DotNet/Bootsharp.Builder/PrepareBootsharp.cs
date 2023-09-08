using Microsoft.Build.Framework;

namespace Bootsharp.Builder;

public class PrepareBootsharp : Microsoft.Build.Utilities.Task
{
    [Required] public required string SerializerContextFilePath { get; set; }
    [Required] public required string InspectedDirectory { get; set; }

    public override bool Execute ()
    {
        GenerateSerializerContext();
        return true;
    }

    private void GenerateSerializerContext ()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(SerializerContextFilePath)!);
        File.WriteAllText(SerializerContextFilePath,
            $$"""
              // InspectedDirectory: {{InspectedDirectory}}
              // {{string.Join("\n// ", Directory.GetFiles(InspectedDirectory, "*.dll"))}}

              using System.Text.Json;
              using System.Text.Json.Serialization;

              namespace Bootsharp;

              [JsonSerializable(typeof(global::Info))]
              public partial class SerializerContext : JsonSerializerContext
              {
                  [System.Runtime.CompilerServices.ModuleInitializer]
                  internal static void InjectTypeInfoResolver ()
                  {
                      Serializer.Options.TypeInfoResolver = SerializerContext.Default;
                  }
              }
              """);
    }
}
