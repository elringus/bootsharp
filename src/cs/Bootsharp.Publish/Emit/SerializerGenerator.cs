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
        var metas = inspection.StaticMethods
            .Concat(inspection.StaticInterfaces.SelectMany(i => i.Methods))
            .Concat(inspection.InstancedInterfaces.SelectMany(i => i.Methods));
        foreach (var meta in metas)
            CollectAttributes(meta);
        CollectDuplicates(inspection);
        if (attributes.Count == 0) return "";
        return
            $$"""
              using System.Text.Json;
              using System.Text.Json.Serialization;

              namespace Bootsharp.Generated;

              {{JoinLines(attributes, 0)}}
              internal partial class SerializerContext : JsonSerializerContext
              {
                  [System.Runtime.CompilerServices.ModuleInitializer]
                  internal static void InjectTypeInfoResolver ()
                  {
                      Serializer.Options.TypeInfoResolverChain.Add(SerializerContext.Default);
                  }
              }
              """;
    }

    private void CollectAttributes (MethodMeta method)
    {
        if (method.ReturnValue.Serialized)
            CollectAttributes(method.ReturnValue.TypeSyntax, method.ReturnValue.Type);
        foreach (var arg in method.Arguments)
            if (arg.Value.Serialized)
                CollectAttributes(arg.Value.TypeSyntax, arg.Value.Type);
    }

    private void CollectAttributes (string syntax, Type type)
    {
        if (IsTaskWithResult(type, out var result))
            // Task<> produces trim warnings, so hacking with a proxy tuple.
            // Passing just the result may conflict with a type inferred by
            // .NET's generator from other types (it throws on duplicates).
            syntax = $"({BuildSyntax(result)}, byte)";
        attributes.Add(BuildAttribute(syntax));
    }

    private void CollectDuplicates (SolutionInspection inspection)
    {
        var names = new HashSet<string>();
        foreach (var type in inspection.Crawled.DistinctBy(t => t.FullName))
            if (ShouldSerialize(type) && !names.Add(type.Name))
                CollectAttributes(BuildSyntax(type), type);
    }

    private static string BuildAttribute (string syntax)
    {
        syntax = syntax.Replace("?", "");
        var hint = $"X{syntax.GetHashCode():X}";
        return $"[JsonSerializable(typeof({syntax}), TypeInfoPropertyName = \"{hint}\")]";
    }
}
