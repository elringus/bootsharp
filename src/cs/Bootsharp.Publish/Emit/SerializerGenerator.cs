namespace Bootsharp.Publish;

internal sealed class SerializerGenerator
{
    private readonly HashSet<string> attributes = [];

    public string Generate (AssemblyInspection inspection)
    {
        foreach (var method in inspection.Methods)
            CollectAttributes(method);
        CollectDuplicates(inspection);
        if (attributes.Count == 0) return "";
        return
            $$"""
              using System.Text.Json;
              using System.Text.Json.Serialization;

              namespace Bootsharp;

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
        AddProxies(type);
        attributes.Add(BuildAttribute(syntax));
    }

    private void CollectDuplicates (AssemblyInspection inspection)
    {
        var names = new HashSet<string>();
        foreach (var type in inspection.Types.DistinctBy(t => t.FullName))
            if (!names.Add(type.Name))
                CollectAttributes(BuildSyntax(type), type);
    }

    private static string BuildAttribute (string syntax)
    {
        syntax = syntax.Replace("?", "");
        var hint = $"X{syntax.GetHashCode():X}";
        return $"[JsonSerializable(typeof({syntax}), TypeInfoPropertyName = \"{hint}\")]";
    }

    private void AddProxies (Type type)
    {
        if (IsTaskWithResult(type, out var result)) type = result;
        if (IsListInterface(type)) AddListProxies(type);
        if (IsDictInterface(type)) AddDictProxies(type);
    }

    private void AddListProxies (Type list)
    {
        var element = BuildSyntax(list.GenericTypeArguments[0]);
        attributes.Add(BuildAttribute($"{element}[]"));
        attributes.Add(BuildAttribute($"global::System.Collections.Generic.List<{element}>"));
    }

    private void AddDictProxies (Type dict)
    {
        var key = BuildSyntax(dict.GenericTypeArguments[0]);
        var value = BuildSyntax(dict.GenericTypeArguments[1]);
        attributes.Add(BuildAttribute($"global::System.Collections.Generic.Dictionary<{key}, {value}>"));
    }

    private static bool IsListInterface (Type type) =>
        type.IsInterface && type.IsGenericType &&
        (type.GetGenericTypeDefinition().FullName == typeof(IList<>).FullName ||
         type.GetGenericTypeDefinition().FullName == typeof(IReadOnlyList<>).FullName);

    private static bool IsDictInterface (Type type) =>
        type.IsInterface && type.IsGenericType &&
        (type.GetGenericTypeDefinition().FullName == typeof(IDictionary<,>).FullName ||
         type.GetGenericTypeDefinition().FullName == typeof(IReadOnlyDictionary<,>).FullName);
}
