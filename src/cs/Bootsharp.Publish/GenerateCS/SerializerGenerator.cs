namespace Bootsharp.Publish;

internal sealed class SerializerGenerator
{
    public string Generate (IReadOnlyCollection<SerializedMeta> srd) => srd.Count == 0 ? "" :
        $$"""
          using System.Runtime.CompilerServices;

          namespace Bootsharp.Generated;

          internal static class SerializerContext
          {
              {{Fmt(srd.Select(EmitFactory))}}

              {{Fmt(srd.OfType<SerializedInstanceMeta>().Select(EmitInstanced), separator: "\n\n")}}

              {{Fmt(srd.OfType<SerializedObjectMeta>().SelectMany(EmitObject), separator: "\n\n")}}
          }
          """;

    private string EmitFactory (SerializedMeta meta)
    {
        return $"internal static readonly Binary<{meta.Syntax}> {meta.Id} = {meta switch {
            SerializedEnumMeta => $"Serializer.Enum<{meta.Syntax}>()",
            SerializedNullableMeta nullable => $"Serializer.Nullable({nullable.Value.Id})",
            SerializedArrayMeta arr => $"Serializer.Array({arr.Element.Id})",
            SerializedListMeta list => $"Serializer.{TrimGeneric(list.Clr.Name)}({list.Element.Id})",
            SerializedDictionaryMeta dic => $"Serializer.{TrimGeneric(dic.Clr.Name)}({dic.Key.Id}, {dic.Value.Id})",
            SerializedObjectMeta or SerializedInstanceMeta => $"new(Write_{meta.Id}, Read_{meta.Id})",
            _ => ResolvePrimitive(meta.Clr)
        }};";

        static string ResolvePrimitive (Type type)
        {
            if (type.FullName == typeof(nint).FullName) return "Serializer.IntPtr";
            if (type.FullName == typeof(DateTimeOffset).FullName) return "Serializer.DateTimeOffset";
            return $"Serializer.{Type.GetTypeCode(type)}";
        }
    }

    private string EmitInstanced (SerializedInstanceMeta it) =>
        $$"""
          private static void Write_{{it.Id}} (ref Writer writer, {{it.Syntax}} value)
          {
              writer.WriteInt32({{Export(it.Instance, "value")}});
          }

          private static {{it.Syntax}} Read_{{it.Id}} (ref Reader reader)
          {
              return {{Import(it.Instance, "reader.ReadInt32()")}};
          }
          """;

    private IEnumerable<string> EmitObject (SerializedObjectMeta obj)
    {
        yield return
            $$"""
              private static void Write_{{obj.Id}} (ref Writer writer, {{obj.Syntax}} value)
              {
                  {{Fmt(EmitObjectWrite(obj))}}
              }

              private static {{obj.Syntax}} Read_{{obj.Id}} (ref Reader reader)
              {
                  {{Fmt(EmitObjectRead(obj))}}
              }
              """;
        foreach (var p in obj.Properties.Where(p => p.Kind == SerializedPropertyKind.Field))
            yield return EmitFieldAccessor(obj, p);
    }

    private IEnumerable<string> EmitObjectWrite (SerializedObjectMeta obj)
    {
        if (!obj.Clr.IsValueType)
        {
            yield return "writer.WriteBool(value is not null);";
            yield return "if (value is null) return;";
        }
        foreach (var p in obj.Properties)
            if (p.Nullable)
            {
                yield return $"writer.WriteBool(value.{p.Name} is not null);";
                yield return $"if (value.{p.Name} is not null) {p.Type.Id}.Write(ref writer, value.{p.Name});";
            }
            else yield return $"{p.Type.Id}.Write(ref writer, value.{p.Name});";
    }

    private IEnumerable<string> EmitObjectRead (SerializedObjectMeta obj)
    {
        if (!obj.Clr.IsValueType) yield return "if (!reader.ReadBool()) return null!;";
        foreach (var p in obj.Properties)
            if (!p.Nullable) yield return $"var {Var(p)} = {p.Type.Id}.Read(ref reader);";
            else yield return $"var {Var(p)} = reader.ReadBool() ? {p.Type.Id}.Read(ref reader) : default;";
        yield return $"var _value_ = {EmitObjectConstruction(obj)};";
        foreach (var p in obj.Properties.Where(p => !p.Ctor && !ShouldInitializeInConstruction(p)))
            if (p.Kind == SerializedPropertyKind.Set) yield return $"_value_.{p.Name} = {Var(p)};";
            else if (p.Kind == SerializedPropertyKind.Field) yield return EmitFieldAssign(obj, p);
        yield return "return _value_;";
    }

    private string EmitObjectConstruction (SerializedObjectMeta obj)
    {
        var ctorArgs = obj.Properties.Where(p => p.Ctor);
        var ctor = $"new {obj.Syntax}({string.Join(", ", ctorArgs.Select(Var))})";
        var props = obj.Properties.Where(p => !p.Ctor && ShouldInitializeInConstruction(p))
            .Select(p => $"{p.Name} = {Var(p)}").ToArray();
        if (props.Length == 0) return ctor;
        return $"{ctor} {{ {string.Join(", ", props)} }}";
    }

    private static string EmitFieldAccessor (SerializedObjectMeta obj, SerializedPropertyMeta prop)
    {
        var value = obj.Clr.IsValueType ? $"ref {obj.Syntax} value" : $"{obj.Syntax} value";
        return $"""
                [UnsafeAccessor(UnsafeAccessorKind.Field, Name = "<{prop.Name}>k__BackingField")]
                private static extern ref {prop.Type.Syntax} {prop.FieldAccessor} ({value});
                """;
    }

    private static string EmitFieldAssign (SerializedObjectMeta obj, SerializedPropertyMeta prop)
    {
        var value = obj.Clr.IsValueType ? "ref _value_" : "_value_";
        return $"{prop.FieldAccessor}({value}) = {Var(prop)};";
    }

    private static bool ShouldInitializeInConstruction (SerializedPropertyMeta prop)
    {
        if (prop.Kind == SerializedPropertyKind.Init) return true;
        return prop.Required && prop.Kind == SerializedPropertyKind.Set;
    }

    private static string Var (SerializedPropertyMeta prop)
    {
        return $"@{ToFirstLower(prop.Name)}";
    }
}
