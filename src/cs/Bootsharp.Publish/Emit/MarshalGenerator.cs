using System.Reflection;

namespace Bootsharp.Publish;

internal class MarshalGenerator
{
    private readonly Dictionary<string, string> generatedByName = [];

    public string Marshal (Type type)
    {
        if (IsNestedArrayWorkaround(type)) return "";
        var methodName = $"Marshal_{GetMarshalId(type)}";
        if (generatedByName.ContainsKey(methodName)) return methodName;
        generatedByName[methodName] = GenerateMarshalMethod(methodName, type);
        return methodName;
    }

    public string Unmarshal (Type type)
    {
        if (IsNestedArrayWorkaround(type)) return "";
        var methodName = $"Unmarshal_{GetMarshalId(type)}";
        if (generatedByName.ContainsKey(methodName)) return methodName;
        generatedByName[methodName] = GenerateUnmarshalMethod(methodName, type);
        return methodName;
    }

    public IReadOnlyCollection<string> GetGenerated () => generatedByName.Values;

    private bool IsNestedArrayWorkaround (Type type) =>
        // TODO: Remove once solved https://github.com/elringus/bootsharp/issues/138.
        type.IsArray && !ShouldMarshall(type.GetElementType()!);

    private string GenerateMarshalMethod (string name, Type type)
    {
        return $"private static object {name} ({BuildSyntax(type)} obj) => {MarshalValue("obj", type)};";

        string MarshalValue (string name, Type type)
        {
            var nullable = IsNullable(type) || !type.IsValueType;
            var template = nullable ? $"{name} is null ? null : (##)" : "##";
            if (!ShouldMarshall(type)) return BuildTemplate(name);
            if (IsList(type)) return BuildTemplate(MarshalList(name, type));
            if (IsDictionary(type)) return BuildTemplate(MarshalDictionary(name, type));
            return BuildTemplate(MarshalStruct(name, type));

            string BuildTemplate (string expression) => template.Replace("##", expression);
        }

        string MarshalList (string name, Type type)
        {
            var elementType = GetListElementType(type);
            if (!ShouldMarshall(elementType)) return $"{name}.ToArray()";
            return $"{name}.Select({Marshal(elementType)}).ToArray()";
        }

        string MarshalDictionary (string name, Type type)
        {
            var keyType = GetDictionaryKeyType(type);
            var valType = GetDictionaryValueType(type);
            var keys = ShouldMarshall(keyType) ? $"{name}.Keys.Select({Marshal(keyType)})" : $"{name}.Keys";
            var vals = ShouldMarshall(valType) ? $"{name}.Values.Select({Marshal(valType)})" : $"{name}.Values";
            return $"(object[])[..{keys}, ..{vals}]";
        }

        string MarshalStruct (string name, Type type)
        {
            // TODO: ... <-----------------------------------------------
            var props = GetMarshaledProperties(type).Select(MarshalProperty);
            return $"new object[] {{ {string.Join(", ", props)} }};";
        }

        string MarshalProperty (PropertyInfo prop)
        {
            var type = prop.PropertyType;
            if (!ShouldMarshall(type)) return $"obj.{prop.Name}";
            return "";
        }
    }

    private string GenerateUnmarshalMethod (string name, Type type)
    {
        var stx = BuildSyntax(type);
        return
            $$"""
              private static {{stx}} {{name}} (object raw)
              {
                  
              }
              """;
    }
}
