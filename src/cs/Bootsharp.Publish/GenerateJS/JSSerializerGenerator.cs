namespace Bootsharp.Publish;

internal sealed class JSSerializerGenerator
{
    public string Generate (IReadOnlyCollection<SerializedMeta> srd) =>
        $$"""
          import $i from "./instances.g.mjs";
          import $s from "../serialization/index.mjs";

          export const { serialize, deserialize } = $s;

          {{Fmt([
              ..srd.Select(EmitFactory),
              ..srd.OfType<SerializedInstanceMeta>().Select(EmitInstanced),
              ..srd.OfType<SerializedObjectMeta>().Select(EmitObject)
          ], 0)}}

          export default $s;
          """;

    private string EmitFactory (SerializedMeta meta) =>
        $"$s.{meta.Id} = {meta switch {
            SerializedEnumMeta => "$s.std.Int32",
            SerializedNullableMeta nullable => $"$s.std.Nullable($s.{nullable.Value.Id})",
            SerializedArrayMeta arr => $"$s.std.Array($s.{arr.Element.Id})",
            SerializedListMeta list => $"$s.std.List($s.{list.Element.Id})",
            SerializedDictionaryMeta dic => $"$s.std.Dictionary($s.{dic.Key.Id}, $s.{dic.Value.Id})",
            SerializedObjectMeta or SerializedInstanceMeta => $"$s.binary(write_{meta.Id}, read_{meta.Id})",
            _ => ResolvePrimitive(meta.Clr)
        }};";

    private static string ResolvePrimitive (Type type) =>
        type.FullName == typeof(nint).FullName ? "$s.std.IntPtr" :
        type.FullName == typeof(DateTimeOffset).FullName ? "$s.std.DateTimeOffset" :
        $"$s.std.{Type.GetTypeCode(type)}";

    private string EmitInstanced (SerializedInstanceMeta it) =>
        $$"""
          function write_{{it.Id}}(writer, value) {
              writer.writeInt32({{ImportJS(it.Instance, "value")}});
          }
          function read_{{it.Id}}(reader) {
              return {{ExportJS(it.Instance, "reader.readInt32()")}};
          }
          """;

    private string EmitObject (SerializedObjectMeta obj) =>
        $$"""
          function write_{{obj.Id}}(writer, value) {
              {{Fmt(EmitObjectWrite(obj))}}
          }
          function read_{{obj.Id}}(reader) {
              {{Fmt(EmitObjectRead(obj))}}
          }
          """;

    private IEnumerable<string> EmitObjectWrite (SerializedObjectMeta obj)
    {
        if (!obj.Clr.IsValueType)
        {
            yield return "writer.writeBool(value != null);";
            yield return "if (value == null) return;";
        }
        foreach (var p in obj.Properties)
            if (p.Nullable)
            {
                yield return $"writer.writeBool(value.{p.JSName} != null);";
                yield return $"if (value.{p.JSName} != null) $s.{p.Type.Id}.write(writer, value.{p.JSName});";
            }
            else yield return $"$s.{p.Type.Id}.write(writer, value.{p.JSName});";
    }

    private IEnumerable<string> EmitObjectRead (SerializedObjectMeta obj)
    {
        if (!obj.Clr.IsValueType) yield return "if (!reader.readBool()) return null;";
        yield return "const value = {};";
        foreach (var p in obj.Properties)
            if (p.Nullable) yield return $"if (reader.readBool()) value.{p.JSName} = $s.{p.Type.Id}.read(reader);";
            else yield return $"value.{p.JSName} = $s.{p.Type.Id}.read(reader);";
        yield return "return value;";
    }
}
