namespace Bootsharp.Publish;

internal sealed class SerializerGenerator
{
    public string Generate (SolutionInspection inspection)
    {
        var serialized = inspection.Serialized;
        if (serialized.Count == 0) return "";
        return $$"""
                 using System.Runtime.CompilerServices;

                 namespace Bootsharp.Generated;

                 internal static class SerializerContext
                 {
                     {{JoinLines(serialized.Select(EmitFactory))}}

                     {{JoinLines(serialized.SelectMany(EmitHelpers), separator: "\n\n")}}
                 }
                 """;
    }

    private string EmitFactory (SerializedMeta meta)
    {
        return $"internal static readonly Binary<{meta.Syntax}> {meta.Id} = {meta switch {
            SerializedEnumMeta => $"Serializer.Enum<{meta.Syntax}>()",
            SerializedNullableMeta nullable => $"Serializer.Nullable({nullable.Value.Id})",
            SerializedArrayMeta arr => $"Serializer.Array({arr.Element.Id})",
            SerializedListMeta list => $"Serializer.{TrimGenericArgs(list.Type.Name)}({list.Element.Id})",
            SerializedDictionaryMeta dic => $"Serializer.{TrimGenericArgs(dic.Type.Name)}({dic.Key.Id}, {dic.Value.Id})",
            SerializedObjectMeta => $"new(Write_{meta.Id}, Read_{meta.Id})",
            _ => ResolvePrimitive(meta.Type)
        }};";

        static string ResolvePrimitive (Type type)
        {
            if (type.FullName == typeof(nint).FullName) return "Serializer.IntPtr";
            if (type.FullName == typeof(DateTimeOffset).FullName) return "Serializer.DateTimeOffset";
            return $"Serializer.{Type.GetTypeCode(type)}";
        }
    }

    private IEnumerable<string> EmitHelpers (SerializedMeta meta)
    {
        if (meta is not SerializedObjectMeta obj) yield break;
        yield return $$"""
                       private static void Write_{{obj.Id}} (ref Writer writer, {{obj.Syntax}} value)
                       {
                           {{JoinLines(EmitObjectWrite(obj))}}
                       }
                       """;
        yield return $$"""
                       private static {{obj.Syntax}} Read_{{obj.Id}} (ref Reader reader)
                       {
                           {{JoinLines(EmitObjectRead(obj))}}
                       }
                       """;
        foreach (var prop in obj.Properties.Where(p => p.Kind == SerializedPropertyKind.Field))
            yield return EmitFieldAccessor(obj, prop);
    }

    private IEnumerable<string> EmitObjectWrite (SerializedObjectMeta obj)
    {
        if (!obj.Type.IsValueType)
        {
            yield return "writer.WriteBool(value is not null);";
            yield return "if (value is null) return;";
        }
        foreach (var p in obj.Properties)
            if (p.OmitWhenNull)
            {
                yield return $"writer.WriteBool(value.{p.Name} is not null);";
                yield return $"if (value.{p.Name} is not null) {p.Id}.Write(ref writer, value.{p.Name});";
            }
            else yield return $"{p.Id}.Write(ref writer, value.{p.Name});";
    }

    private IEnumerable<string> EmitObjectRead (SerializedObjectMeta obj)
    {
        if (!obj.Type.IsValueType) yield return "if (!reader.ReadBool()) return null!;";
        foreach (var p in obj.Properties)
        {
            var var = MangleLocal(p.Name);
            if (p.OmitWhenNull) yield return $"var {var} = reader.ReadBool() ? {p.Id}.Read(ref reader) : default;";
            else yield return $"var {var} = {p.Id}.Read(ref reader);";
        }
        yield return $"var _value_ = {EmitObjectConstruction(obj)};";
        foreach (var p in obj.Properties.Where(p => !p.ConstructorParameter && !ShouldInitializeInConstruction(p)))
        {
            var var = MangleLocal(p.Name);
            if (p.Kind == SerializedPropertyKind.Set) yield return $"_value_.{p.Name} = {var};";
            else if (p.Kind == SerializedPropertyKind.Field) yield return EmitFieldAssign(obj, p);
        }
        yield return "return _value_;";
    }

    private string EmitObjectConstruction (SerializedObjectMeta obj)
    {
        var ctorArgs = obj.Properties.Where(p => p.ConstructorParameter);
        var ctor = $"new {obj.Syntax}({string.Join(", ", ctorArgs.Select(p => MangleLocal(p.Name)))})";
        var props = obj.Properties.Where(p => !p.ConstructorParameter && ShouldInitializeInConstruction(p))
            .Select(p => $"{p.Name} = {MangleLocal(p.Name)}").ToArray();
        if (props.Length == 0) return ctor;
        return $"{ctor} {{ {string.Join(", ", props)} }}";
    }

    private static string EmitFieldAccessor (SerializedObjectMeta obj, SerializedPropertyMeta prop)
    {
        var value = obj.Type.IsValueType ? $"ref {obj.Syntax} value" : $"{obj.Syntax} value";
        return $"""
                [UnsafeAccessor(UnsafeAccessorKind.Field, Name = "<{prop.Name}>k__BackingField")]
                private static extern ref {prop.Syntax} {prop.FieldAccessorName} ({value});
                """;
    }

    private static string EmitFieldAssign (SerializedObjectMeta obj, SerializedPropertyMeta prop)
    {
        var value = obj.Type.IsValueType ? "ref _value_" : "_value_";
        return $"{prop.FieldAccessorName}({value}) = {MangleLocal(prop.Name)};";
    }

    private static bool ShouldInitializeInConstruction (SerializedPropertyMeta prop)
    {
        if (prop.Kind == SerializedPropertyKind.Init) return true;
        return prop.Required && prop.Kind == SerializedPropertyKind.Set;
    }

    private static string MangleLocal (string name)
    {
        return $"@{ToFirstLower(name)}";
    }
}
