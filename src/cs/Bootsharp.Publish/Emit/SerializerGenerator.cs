using System.Text;

namespace Bootsharp.Publish;

internal sealed class SerializerGenerator
{
    public string Generate (SolutionInspection inspection)
    {
        var meta = inspection.Serialized;
        if (meta.Count == 0) return "";
        return $$"""
                 using System.Runtime.CompilerServices;

                 namespace Bootsharp.Generated;

                 internal static class SerializerContext
                 {
                     {{JoinLines(meta.Select(EmitType))}}

                     {{JoinLines(meta.OfType<SerializedObjectMeta>().SelectMany(EmitObject), separator: "\n\n")}}
                 }
                 """;
    }

    private static string EmitType (SerializedMeta meta)
    {
        return $"internal static readonly Binary<{meta.Syntax}> {meta.Id} = {EmitFactory(meta)};";

        static string EmitFactory (SerializedMeta meta) => meta switch {
            SerializedEnumMeta => $"Serializer.Enum<{meta.Syntax}>()",
            SerializedNullableMeta nullable => $"Serializer.Nullable({nullable.Value.Id})",
            SerializedArrayMeta arr => $"Serializer.Array({arr.Element.Id})",
            SerializedListMeta list => $"Serializer.{GetGeneric(list)}({list.Element.Id})",
            SerializedDictionaryMeta dic => $"Serializer.{GetGeneric(dic)}({dic.Key.Id}, {dic.Value.Id})",
            SerializedObjectMeta => $"new(Write_{meta.Id}, Read_{meta.Id})",
            _ => EmitPrimitive(meta.Type)
        };

        static string GetGeneric (SerializedMeta meta)
        {
            var name = meta.Type.Name;
            return name[..name.IndexOf('`')];
        }

        static string EmitPrimitive (Type type)
        {
            if (type.FullName == typeof(nint).FullName) return "Serializer.IntPtr";
            if (type.FullName == typeof(DateTimeOffset).FullName) return "Serializer.DateTimeOffset";
            return $"Serializer.{Type.GetTypeCode(type)}";
        }
    }

    private static IEnumerable<string> EmitObject (SerializedObjectMeta obj)
    {
        yield return EmitObjectWriter(obj);
        yield return EmitObjectReader(obj);
        foreach (var property in obj.Properties.Where(p => p.Kind == SerializedPropertyKind.Field))
            yield return EmitFieldAccessor(obj, property);
    }

    private static string EmitObjectWriter (SerializedObjectMeta obj)
    {
        var body = new StringBuilder();
        body.AppendLine($"private static void Write_{obj.Id} (ref Writer writer, {obj.Syntax} value)");
        body.AppendLine("{");
        if (!obj.Type.IsValueType)
        {
            body.AppendLine("    writer.WriteBool(value is not null);");
            body.AppendLine("    if (value is null) return;");
        }
        foreach (var property in obj.Properties)
        {
            if (property.OmitWhenNull)
            {
                body.AppendLine($"    writer.WriteBool(value.{property.Name} is not null);");
                body.AppendLine($"    if (value.{property.Name} is not null) {property.Id}.Write(ref writer, value.{property.Name});");
            }
            else body.AppendLine($"    {property.Id}.Write(ref writer, value.{property.Name});");
        }
        body.Append('}');
        return body.ToString();
    }

    private static string EmitObjectReader (SerializedObjectMeta obj)
    {
        var body = new StringBuilder();
        body.AppendLine($"private static {obj.Syntax} Read_{obj.Id} (ref Reader reader)");
        body.AppendLine("{");
        if (!obj.Type.IsValueType)
        {
            body.AppendLine("    if (!reader.ReadBool()) return null!;");
        }
        foreach (var prop in obj.Properties)
        {
            var local = MangleLocal(prop.Name);
            if (prop.OmitWhenNull)
                body.AppendLine($"    var {local} = reader.ReadBool() ? {prop.Id}.Read(ref reader) : default;");
            else body.AppendLine($"    var {local} = {prop.Id}.Read(ref reader);");
        }
        body.AppendLine($"    var _value_ = {BuildObjectConstruction(obj)};");
        foreach (var property in obj.Properties.Where(p => !p.ConstructorParameter && !ShouldInitializeInConstruction(p)))
        {
            var local = MangleLocal(property.Name);
            if (property.Kind == SerializedPropertyKind.Set) body.AppendLine($"    _value_.{property.Name} = {local};");
            else if (property.Kind == SerializedPropertyKind.Field)
                body.AppendLine(obj.Type.IsValueType
                    ? $"    {property.FieldAccessorName}(ref _value_) = {local};"
                    : $"    {property.FieldAccessorName}(_value_) = {local};");
        }
        body.AppendLine("    return _value_;");
        body.Append('}');
        return body.ToString();
    }

    private static string BuildObjectConstruction (SerializedObjectMeta obj)
    {
        var ctorArgs = obj.Properties.Where(p => p.ConstructorParameter);
        var ctor = $"new {obj.Syntax}({string.Join(", ", ctorArgs.Select(p => MangleLocal(p.Name)))})";
        var props = obj.Properties
            .Where(p => !p.ConstructorParameter && ShouldInitializeInConstruction(p))
            .Select(p => $"{p.Name} = {MangleLocal(p.Name)}").ToArray();
        if (props.Length == 0) return ctor;
        return $"{ctor} {{ {string.Join(", ", props)} }}";
    }

    private static string EmitFieldAccessor (SerializedObjectMeta obj, SerializedPropertyMeta property)
    {
        var receiver = obj.Type.IsValueType ? $"ref {obj.Syntax} value" : $"{obj.Syntax} value";
        return $"""
                [UnsafeAccessor(UnsafeAccessorKind.Field, Name = "<{property.Name}>k__BackingField")]
                private static extern ref {property.Syntax} {property.FieldAccessorName} ({receiver});
                """;
    }

    private static bool ShouldInitializeInConstruction (SerializedPropertyMeta property)
    {
        return property.Kind == SerializedPropertyKind.Init ||
               property.Required && property.Kind == SerializedPropertyKind.Set;
    }

    private static string MangleLocal (string name)
    {
        return $"@{ToFirstLower(name)}";
    }
}
