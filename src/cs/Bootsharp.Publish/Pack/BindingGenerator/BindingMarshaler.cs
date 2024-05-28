﻿namespace Bootsharp.Publish;

internal sealed class BindingMarshaler
{
    private readonly Dictionary<string, string> generatedByName = [];

    public string Marshal (Type type)
    {
        if (IsTaskWithResult(type, out var result)) type = result;
        if (ShouldMarshalPassThrough(type)) return "";
        var fnName = $"marshal_{GetMarshalId(type)}";
        if (generatedByName.ContainsKey(fnName)) return fnName;
        generatedByName[fnName] = GenerateMarshalFunction(fnName, type);
        return fnName;
    }

    public string Unmarshal (Type type)
    {
        if (IsTaskWithResult(type, out var result)) type = result;
        if (ShouldMarshalPassThrough(type)) return "";
        var fnName = $"unmarshal_{GetMarshalId(type)}";
        if (generatedByName.ContainsKey(fnName)) return fnName;
        generatedByName[fnName] = GenerateUnmarshalFunction(fnName, type);
        return fnName;
    }

    public IReadOnlyCollection<string> GetGenerated () => generatedByName.Values;

    private string GenerateMarshalFunction (string fnName, Type marshaledType)
    {
        return $"function {fnName}(obj) {{ return {MarshalValue("obj", marshaledType)}; }}";

        string MarshalValue (string name, Type valueType)
        {
            var nullable = IsNullable(valueType) || !valueType.IsValueType;
            var template = nullable ? $"{name} == null ? null : ##" : "##";
            if (!ShouldMarshal(valueType)) return BuildTemplate(name);
            if (valueType.IsEnum) return BuildTemplate(name);
            if (IsList(valueType)) return BuildTemplate(MarshalList(name, valueType));
            if (IsDictionary(valueType)) return BuildTemplate(MarshalDictionary(name, valueType));
            return BuildTemplate(MarshalStruct(name, valueType));

            string BuildTemplate (string expression) => template.Replace("##", expression);
        }

        string MarshalList (string name, Type listType)
        {
            var elementType = GetListElementType(listType);
            if (!ShouldMarshal(elementType)) return name;
            return $"{name}.map({Marshal(elementType)})";
        }

        string MarshalDictionary (string name, Type dictType)
        {
            var keyType = GetDictionaryKeyType(dictType);
            var valType = GetDictionaryValueType(dictType);
            var keys = ShouldMarshal(keyType) ? $"Array.from({name}.keys(), {Marshal(keyType)})" : $"{name}.keys()";
            var vals = ShouldMarshal(valType) ? $"Array.from({name}.values(), {Marshal(valType)})" : $"{name}.values()";
            return $"[...{keys}, ...{vals}]";
        }

        string MarshalStruct (string name, Type structType)
        {
            if (structType != marshaledType) return $"{Marshal(structType)}({name})";
            var props = GetMarshaledProperties(structType)
                .Select(p => MarshalValue($"{name}.{ToFirstLower(p.Name)}", p.PropertyType));
            return $"[ {string.Join(", ", props)} ]";
        }
    }

    private string GenerateUnmarshalFunction (string fnName, Type marshaledType)
    {
        return $"function {fnName}(raw) {{ return {UnmarshalValue("raw", marshaledType)}; }}";

        string UnmarshalValue (string name, Type valueType)
        {
            var nullable = IsNullable(valueType) || !valueType.IsValueType;
            var template = nullable ? $"{name} == null ? undefined : ##" : "##";
            if (valueType.IsEnum) return BuildTemplate(name);
            if (IsList(valueType)) return BuildTemplate(UnmarshalList(name, valueType));
            if (IsDictionary(valueType)) return BuildTemplate(UnmarshalDictionary(name, valueType));
            if (ShouldMarshal(valueType)) return BuildTemplate(UnmarshalStruct(name, valueType));
            return BuildTemplate(name);

            string BuildTemplate (string expression) => template.Replace("##", expression);
        }

        string UnmarshalList (string name, Type listType)
        {
            var elementType = GetListElementType(listType);
            if (!ShouldMarshal(elementType)) return name;
            return $"{name}.map({Unmarshal(elementType)})";
        }

        string UnmarshalDictionary (string name, Type dictType)
        {
            var arr = name;
            var keyType = GetDictionaryKeyType(dictType);
            var valType = GetDictionaryValueType(dictType);
            return $"new Map({arr}.slice(0, {arr}.length / 2).map((obj, idx) => [" +
                   $"{UnmarshalValue("obj", keyType)}, " +
                   $"{UnmarshalValue($"{arr}[idx + {arr}.length / 2]", valType)}" +
                   $"]))";
        }

        string UnmarshalStruct (string name, Type structType)
        {
            if (structType != marshaledType) return $"{Unmarshal(structType)}({name})";
            var props = GetMarshaledProperties(structType).Select((p, idx) =>
                $"{ToFirstLower(p.Name)}: {UnmarshalValue($"{name}[{idx}]", p.PropertyType)}");
            return $"{{ {string.Join(", ", props)} }}";
        }
    }
}