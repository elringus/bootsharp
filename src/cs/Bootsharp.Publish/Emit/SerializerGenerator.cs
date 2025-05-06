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
        CollectTopLevel(inspection);
        CollectCrawled(inspection);
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

    private void CollectTopLevel (SolutionInspection inspection)
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
        attributes.Add(BuildAttribute(meta.Type));
    }

    private void CollectCrawled (SolutionInspection inspection)
    {
        foreach (var type in inspection.Crawled)
            if (ShouldSerialize(type))
                attributes.Add(BuildAttribute(type));
    }

    private static string BuildAttribute (Type type)
    {
        var syntax = IsTaskWithResult(type, out var result) ? BuildSyntax(result) : BuildSyntax(type);
        var info = BuildTypeInfo(type);
        return $"[JsonSerializable(typeof({syntax}), TypeInfoPropertyName = \"{info}\")]";
    }
}
