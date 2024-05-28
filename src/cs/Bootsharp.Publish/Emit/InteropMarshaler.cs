namespace Bootsharp.Publish;

internal sealed class InteropMarshaler
{
    private readonly Dictionary<string, string> generatedByName = [];

    public string Marshal (Type type)
    {
        if (IsTaskWithResult(type, out var result)) type = result;
        if (ShouldMarshalPassThrough(type)) return "";
        var methodName = BuildMarshalMethodName(type);
        if (generatedByName.ContainsKey(methodName)) return methodName;
        generatedByName[methodName] = GenerateMarshalMethod(methodName, type);
        return methodName;
    }

    public string Unmarshal (Type type)
    {
        if (IsTaskWithResult(type, out var result)) type = result;
        if (ShouldMarshalPassThrough(type)) return "";
        var methodName = BuildUnmarshalMethodName(type);
        if (generatedByName.ContainsKey(methodName)) return methodName;
        generatedByName[methodName] = GenerateUnmarshalMethod(methodName, type);
        return methodName;
    }

    public IReadOnlyCollection<string> GetGenerated () => generatedByName.Values;

    private string BuildMarshalMethodName (Type type)
    {
        return $"Marshal_{GetMarshalId(type)}";
    }

    private string BuildUnmarshalMethodName (Type type)
    {
        return $"Unmarshal_{GetMarshalId(type)}";
    }

    private string GenerateMarshalMethod (string methodName, Type marshaledType)
    {
        return $"private static object {methodName} ({BuildSyntax(marshaledType)} obj)" +
               $" => {MarshalValue("obj", marshaledType)};";

        string MarshalValue (string name, Type valueType)
        {
            if (IsRecursive(valueType))
                return $"{BuildMarshalMethodName(valueType)}({name})";

            var nullable = IsNullable(valueType) || !valueType.IsValueType;
            var template = nullable ? $"{name} is null ? null : ##" : "##";
            if (!ShouldMarshal(valueType)) return BuildTemplate(name);
            if (valueType.IsEnum) return BuildTemplate($"({BuildSyntax(Enum.GetUnderlyingType(valueType))}){name}");
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
            if (structType != marshaledType) return $"{Marshal(structType)}({name})";
            var props = GetMarshaledProperties(structType)
                .Select(p => {
                    return MarshalValue($"{name}.{p.Name}", p.PropertyType);
                });
            return $"new object[] {{ {string.Join(", ", props)} }}";
        }
    }

    private string GenerateUnmarshalMethod (string methodName, Type marshaledType)
    {
        return $"private static {BuildSyntax(marshaledType)} {methodName} (object raw)" +
               $" => {UnmarshalValue("raw", marshaledType)};";

        string UnmarshalValue (string name, Type valueType)
        {
            if (IsRecursive(valueType))
                return $"{BuildUnmarshalMethodName(valueType)}({name})";

            var nullable = IsNullable(valueType) || !valueType.IsValueType;
            var template = nullable ? $"{name} is null ? null : ##" : "##";
            if (valueType.IsEnum) return BuildTemplate($"({BuildSyntax(valueType)})({BuildSyntax(Enum.GetUnderlyingType(valueType))})(double){name}");
            if (IsList(valueType)) return BuildTemplate(UnmarshalList(name, valueType));
            if (IsDictionary(valueType)) return BuildTemplate(UnmarshalDictionary(name, valueType));
            if (ShouldMarshal(valueType)) return BuildTemplate(UnmarshalStruct(name, valueType));
            if (IsNonDoubleNum(valueType))
                return valueType == marshaledType
                    ? BuildTemplate($"({BuildSyntax(valueType)})(double){name}")
                    : BuildTemplate($"{Unmarshal(valueType)}({name})");
            return BuildTemplate($"({BuildSyntax(valueType)}){name}");

            string BuildTemplate (string expression) => template.Replace("##", expression);
        }

        string UnmarshalList (string name, Type listType)
        {
            var elementType = GetListElementType(listType);
            if (elementType.FullName!.Contains(typeof(int).FullName!) ||
                elementType.FullName!.Contains(typeof(double).FullName!) ||
                elementType.FullName!.Contains(typeof(byte).FullName!))
                if (listType.IsArray || listType.IsInterface) return $"({BuildSyntax(elementType)}[]){name}";
                else return $"(({BuildSyntax(elementType)}[]){name}).ToList()";
            return $"({BuildSyntax(listType)})[..((object[]){name}).Select(e => {Unmarshal(elementType)}(e))]";
        }

        string UnmarshalDictionary (string name, Type dictType)
        {
            var arr = $"((object[]){name})";
            var keyType = GetDictionaryKeyType(dictType);
            var valType = GetDictionaryValueType(dictType);
            return $"{arr}.Take({arr}.Length / 2).Select((obj, idx) => (" +
                   $"{UnmarshalValue("obj", keyType)}, " +
                   $"{UnmarshalValue($"{arr}[idx + {arr}.Length / 2]", valType)}" +
                   $")).ToDictionary()";
        }

        string UnmarshalStruct (string name, Type structType)
        {
            if (structType != marshaledType) return $"{Unmarshal(structType)}({name})";
            var arr = $"((object[]){name})";
            var ctor = structType.GetConstructors().Any(c => c.GetParameters().Length > 0);
            var args = string.Join(", ", GetMarshaledProperties(structType).Select((p, idx) => {
                var assign = ctor ? "" : $"{p.Name} = ";
                return assign + UnmarshalValue($"{arr}[{idx}]", p.PropertyType);
            }));
            return $"new {BuildSyntax(structType)}" + (ctor ? $"({args})" : $" {{ {args} }}");
        }

        // All numbers in JavaScript are doubles.
        bool IsNonDoubleNum (Type type) =>
            type.IsPrimitive &&
            !type.FullName!.Contains(typeof(double).FullName!) &&
            !type.FullName!.Contains(typeof(bool).FullName!);
    }
}
