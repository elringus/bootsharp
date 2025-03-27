namespace Bootsharp.Publish;

/// <summary>
/// Generates hints for all the types used in interop to be picked by
/// .NET's JSON serializer source generator. Required for the serializer to
/// work without using reflection (which is required to support trimming).
/// </summary>
internal sealed class SerializerGenerator
{
    private readonly HashSet<string> attributes = [];

    public string Generate (SolutionInspection inspection)
    {
        CollectAttributes(inspection);
        CollectDuplicates(inspection);
        if (attributes.Count == 0) return "";
        return
            $"""
             using System.Text.Json.Serialization;

             namespace Bootsharp.Generated;

             {JoinLines(attributes, 0)}
             [JsonSourceGenerationOptions(
                 PropertyNameCaseInsensitive = true,
                 PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
                 DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
             )]
             internal partial class SerializerContext : JsonSerializerContext;
             """;
    }

    private void CollectAttributes (SolutionInspection inspection)
    {
        var metas = inspection.StaticMethods
            .Concat(inspection.StaticInterfaces.SelectMany(i => i.Methods))
            .Concat(inspection.InstancedInterfaces.SelectMany(i => i.Methods));
        foreach (var meta in metas)
            CollectFromMethod(meta);
    }

    private void CollectFromMethod (MethodMeta method)
    {
        if (method.ReturnValue.Serialized)
            CollectFromValue(method.ReturnValue);
        foreach (var arg in method.Arguments)
            if (arg.Value.Serialized)
                CollectFromValue(arg.Value);
    }

    private void CollectFromValue (ValueMeta meta)
    {
        attributes.Add(BuildAttribute(meta.TypeSyntax, meta.TypeInfo));
    }

    private void CollectDuplicates (SolutionInspection inspection)
    {
        var names = new HashSet<string>();
        foreach (var type in inspection.Crawled.DistinctBy(t => t.FullName))
            if (ShouldSerialize(type) && !names.Add(type.Name))
                attributes.Add(BuildAttribute(BuildSyntax(type), BuildTypeInfo(type)));
    }

    private static string BuildAttribute (string typeSyntax, string typeInfo)
    {
        return $"[JsonSerializable(typeof({typeSyntax}), TypeInfoPropertyName = \"{typeInfo}\")]";
    }
}
