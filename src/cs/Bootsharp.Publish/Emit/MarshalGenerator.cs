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
        type.IsArray && !ShouldMarshal(type.GetElementType()!);

    private string GenerateMarshalMethod (string methodName, Type marshalledType)
    {
        return $"private static object {methodName} ({BuildSyntax(marshalledType)} obj)" +
               $" => {MarshalValue("obj", marshalledType)};";

        string MarshalValue (string name, Type valueType)
        {
            var nullable = IsNullable(valueType) || !valueType.IsValueType;
            var template = nullable ? $"{name} is null ? null : ##" : "##";
            if (!ShouldMarshal(valueType)) return BuildTemplate(name);
            if (IsList(valueType)) return BuildTemplate(MarshalList(name, valueType));
            if (IsDictionary(valueType)) return BuildTemplate(MarshalDictionary(name, valueType));
            return BuildTemplate(MarshalStruct(name, valueType));

            string BuildTemplate (string expression) => template.Replace("##", expression);
        }

        string MarshalList (string name, Type listType)
        {
            var elementType = GetListElementType(listType);
            if (!ShouldMarshal(elementType)) return $"{name}.ToArray()";
            return $"{name}.Select({Marshal(elementType)}).ToArray()";
        }

        string MarshalDictionary (string name, Type dictType)
        {
            var keyType = GetDictionaryKeyType(dictType);
            var valType = GetDictionaryValueType(dictType);
            var keys = ShouldMarshal(keyType) ? $"{name}.Keys.Select({Marshal(keyType)})" : $"{name}.Keys";
            var vals = ShouldMarshal(valType) ? $"{name}.Values.Select({Marshal(valType)})" : $"{name}.Values";
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
            if (ShouldMarshal(valueType)) BuildTemplate(UnmarshalStruct(name, valueType));
            if (IsNonDoubleNum(valueType)) return BuildTemplate($"({BuildSyntax(valueType)})(double){name}");
            return BuildTemplate($"({BuildSyntax(valueType)}){name}");

            string BuildTemplate (string expression) => template.Replace("##", expression);
        }

        string UnmarshalList (string name, Type listType)
        {
            var elementType = GetListElementType(listType);
            if (IsNonDoubleNum(elementType))
                return $"({BuildSyntax(listType)})[..((double[]){name}).Select({Unmarshal(elementType)})]";
            if (ShouldMarshal(elementType))
                return $"({BuildSyntax(listType)})[..((object[]){name}).Select({Unmarshal(elementType)})]";
            if (listType == typeof(List<>))
                return $"(({BuildSyntax(elementType)}[]){name}).ToList()";
            return $"({BuildSyntax(elementType)}[]){name}";
        }

        string UnmarshalDictionary (string name, Type dictType)
        {
            var arr = $"((object[]){name})";
            var keyType = GetDictionaryKeyType(dictType);
            var valType = GetDictionaryValueType(dictType);
            var key = ShouldMarshal(keyType) || IsNonDoubleNum(keyType)
                ? $"{Unmarshal(keyType)}(obj)"
                : $"({BuildSyntax(keyType)})obj";
            var val = ShouldMarshal(valType) || IsNonDoubleNum(valType)
                ? $"{Unmarshal(valType)}({arr}[idx + {arr}.Length / 2])"
                : $"({BuildSyntax(valType)}){arr}[idx + {arr}.Length / 2]";
            return $"{arr}.Take({arr}.Length / 2).Select((obj, idx) => ({key}, {val})).ToDictionary()";
        }

        string UnmarshalStruct (string name, Type structType)
        {
            // new Struct { Foo = "" };
            // new Record("");

            return "";

            bool ShouldInitViaCtor (Type type) => type.GetConstructors().Any(c => c.GetParameters().Length > 0);
        }

        // All numbers in JavaScript are doubles.
        bool IsNonDoubleNum (Type type) =>
            type.IsPrimitive &&
            type != typeof(double) &&
            type != typeof(bool);
    }
}
