namespace Bootsharp.Publish;

internal sealed class BindingSerializerGenerator
{
    public string Generate (IReadOnlyCollection<SerializedMeta> serialized)
    {
        if (serialized.Count == 0) return "";
        return $"""
                {JoinLines(serialized.Select(EmitFactory), 0)}

                {JoinLines(serialized.SelectMany(EmitHelpers), 0, "\n\n")}
                """;
    }

    private string EmitFactory (SerializedMeta meta)
    {
        return $"const {meta.Id} = {meta switch {
            SerializedEnumMeta => "types.Int32",
            SerializedNullableMeta nullable => $"types.Nullable({nullable.Value.Id})",
            SerializedArrayMeta arr => $"types.Array({arr.Element.Id})",
            SerializedListMeta list => $"types.List({list.Element.Id})",
            SerializedDictionaryMeta dic => $"types.Dictionary({dic.Key.Id}, {dic.Value.Id})",
            SerializedObjectMeta => $"binary(write_{meta.Id}, read_{meta.Id})",
            _ => ResolvePrimitive(meta.Type)
        }};";

        static string ResolvePrimitive (Type type)
        {
            if (type.FullName == typeof(nint).FullName) return "types.IntPtr";
            if (type.FullName == typeof(DateTimeOffset).FullName) return "types.DateTimeOffset";
            return $"types.{Type.GetTypeCode(type)}";
        }
    }

    private IEnumerable<string> EmitHelpers (SerializedMeta meta)
    {
        if (meta is not SerializedObjectMeta obj) yield break;
        yield return $$"""
                       function write_{{obj.Id}}(writer, value) {
                           {{JoinLines(EmitObjectWrite(obj))}}
                       }
                       """;
        yield return $$"""
                       function read_{{obj.Id}}(reader) {
                           {{JoinLines(EmitObjectRead(obj))}}
                       }
                       """;
    }

    private IEnumerable<string> EmitObjectWrite (SerializedObjectMeta obj)
    {
        if (!obj.Type.IsValueType)
        {
            yield return "writer.writeBool(value != null);";
            yield return "if (value == null) return;";
        }
        foreach (var p in obj.Properties)
            if (p.OmitWhenNull)
            {
                yield return $"writer.writeBool(value.{p.JSName} != null);";
                yield return $"if (value.{p.JSName} != null) {p.Id}.write(writer, value.{p.JSName});";
            }
            else yield return $"{p.Id}.write(writer, value.{p.JSName});";
    }

    private IEnumerable<string> EmitObjectRead (SerializedObjectMeta obj)
    {
        if (!obj.Type.IsValueType) yield return "if (!reader.readBool()) return null;";
        yield return "const value = {};";
        foreach (var p in obj.Properties)
            if (p.OmitWhenNull) yield return $"if (reader.readBool()) value.{p.JSName} = {p.Id}.read(reader);";
            else yield return $"value.{p.JSName} = {p.Id}.read(reader);";
        yield return "return value;";
    }
}
