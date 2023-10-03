namespace Bootsharp.Builder;

internal sealed class SerializerGenerator
{
    private readonly HashSet<string> attributes = new();

    public string Generate (AssemblyInspector inspector)
    {
        inspector.Methods.ForEach(CollectAttributes);
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
                      Serializer.Options.TypeInfoResolver = SerializerContext.Default;
                  }
              }
              """;
    }

    private void CollectAttributes (Method method)
    {
        if (method.ShouldSerializeReturnType)
            CollectAttributes(method.ReturnTypeSyntax, method.ReturnType);
        foreach (var arg in method.Arguments)
            if (arg.ShouldSerialize)
                CollectAttributes(arg.TypeSyntax, arg.Type);
    }

    private void CollectAttributes (string syntax, Type type)
    {
        attributes.Add(BuildAttribute(syntax, type));
        if (IsListInterface(type)) AddListProxies(type);
        if (IsDictInterface(type)) AddDictProxies(type);
    }

    private static string BuildAttribute (string syntax, Type type)
    {
        syntax = syntax.Replace("?", "");
        var hint = $"X{syntax.GetHashCode():X}";
        return $"[JsonSerializable(typeof({syntax}), TypeInfoPropertyName = \"{hint}\")]";
    }

    private void AddListProxies (Type list)
    {
        var element = $"global::{list.GenericTypeArguments[0].FullName}";
        attributes.Add(BuildAttribute($"{element}[]", list));
        attributes.Add(BuildAttribute($"global::System.Collections.Generic.List<{element}>", list));
    }

    private void AddDictProxies (Type dict)
    {
        var key = $"global::{dict.GenericTypeArguments[0].FullName}";
        var value = $"global::{dict.GenericTypeArguments[1].FullName}";
        attributes.Add(BuildAttribute($"global::System.Collections.Generic.Dictionary<{key}, {value}>", dict));
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
