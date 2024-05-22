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

    private string GenerateMarshalMethod (string methodName, Type marshalledType)
    {
        return $"private static object {methodName} ({BuildSyntax(marshalledType)} obj)" +
               $" => {MarshalValue("obj", marshalledType)};";

        string MarshalValue (string name, Type valueType)
        {
            var nullable = IsNullable(valueType) || !valueType.IsValueType;
            var template = nullable ? $"{name} is null ? null : ##" : "##";
            if (!ShouldMarshall(valueType)) return BuildTemplate(name);
            if (IsList(valueType)) return BuildTemplate(MarshalList(name, valueType));
            if (IsDictionary(valueType)) return BuildTemplate(MarshalDictionary(name, valueType));
            return BuildTemplate(MarshalStruct(name, valueType));

            string BuildTemplate (string expression) => template.Replace("##", expression);
        }

        string MarshalList (string name, Type listType)
        {
            var elementType = GetListElementType(listType);
            if (!ShouldMarshall(elementType)) return $"{name}.ToArray()";
            return $"{name}.Select({Marshal(elementType)}).ToArray()";
        }

        string MarshalDictionary (string name, Type dictType)
        {
            var keyType = GetDictionaryKeyType(dictType);
            var valType = GetDictionaryValueType(dictType);
            var keys = ShouldMarshall(keyType) ? $"{name}.Keys.Select({Marshal(keyType)})" : $"{name}.Keys";
            var vals = ShouldMarshall(valType) ? $"{name}.Values.Select({Marshal(valType)})" : $"{name}.Values";
            return $"(object[])[..{keys}, ..{vals}]";
        }

        string MarshalStruct (string name, Type structType)
        {
            if (structType != marshalledType) return $"{Marshal(structType)}({name})";
            var props = GetMarshaledProperties(structType)
                .Select(p => MarshalValue($"{name}.{p.Name}", p.PropertyType));
            return $"new object[] {{ {string.Join(", ", props)} }}";
        }
    }

    private string GenerateUnmarshalMethod (string methodName, Type marshalledType)
    {
        return $"private static {BuildSyntax(marshalledType)} {methodName} (object raw) " +
               $" => {UnmarshalValue("raw", marshalledType)};";

        string UnmarshalValue (string name, Type valueType)
        {
            var nullable = IsNullable(valueType) || !valueType.IsValueType;
            var template = nullable ? $"{name} is null ? null : ##" : "##";
            if (IsList(valueType)) return BuildTemplate(UnmarshalList(name, valueType));
            if (IsDictionary(valueType)) return BuildTemplate(UnmarshalDictionary(name, valueType));
            if (!ShouldUnmarshall(valueType)) return BuildTemplate($"({BuildSyntax(valueType)}){name}");
            return BuildTemplate(UnmarshalStruct(name, valueType));

            string BuildTemplate (string expression) => template.Replace("##", expression);
        }

        string UnmarshalList (string name, Type listType)
        {
            var elementType = GetListElementType(listType);
            var elementSyntax = BuildSyntax(elementType);
            var syntax = ShouldUnmarshall(elementType)
                ? $"((object[])[..(System.Collections.IList){name}]).Select({Unmarshal(elementType)})"
                : $"({elementSyntax}[]){name}";
            if (listType == typeof(List<>)) return $"({syntax}).ToList()";
            return ShouldUnmarshall(elementType) ? $"{syntax}.ToArray()" : syntax;
        }

        string UnmarshalDictionary (string name, Type dictType)
        {
            return "";
        }

        string UnmarshalStruct (string name, Type structType)
        {
            // new Struct { Foo = "" };
            // new Record("");

            return "";

            bool ShouldInitViaCtor (Type type) => type.GetConstructors().Any(c => c.GetParameters().Length > 0);
        }
    }
}
