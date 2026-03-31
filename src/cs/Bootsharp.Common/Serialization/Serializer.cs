using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Bootsharp;

/// <summary>
/// Handles binary serialization of the interop values that can't be passed to and from JavaScript as-is.
/// </summary>
public static class Serializer
{
    public static long Serialize<T> (T? value, Binary<T> type)
    {
        if (value is null) return 0;
        var writer = new Writer();
        type.Write(ref writer, value);
        return writer.Detach();
    }

    public static T? Deserialize<T> (long handle, Binary<T> type)
    {
        if (handle == 0) return default;
        var reader = new Reader(handle);
        var result = type.Read(ref reader);
        reader.Dispose();
        return result;
    }

    public static readonly Binary<bool> Boolean = new(
        static (ref writer, value) => writer.WriteBool(value),
        static (ref reader) => reader.ReadBool());

    public static readonly Binary<byte> Byte = new(
        static (ref writer, value) => writer.WriteByte(value),
        static (ref reader) => reader.ReadByte());

    public static readonly Binary<sbyte> SByte = new(
        static (ref writer, value) => writer.WriteSByte(value),
        static (ref reader) => reader.ReadSByte());

    public static readonly Binary<short> Int16 = new(
        static (ref writer, value) => writer.WriteInt16(value),
        static (ref reader) => reader.ReadInt16());

    public static readonly Binary<ushort> UInt16 = new(
        static (ref writer, value) => writer.WriteUInt16(value),
        static (ref reader) => reader.ReadUInt16());

    public static readonly Binary<int> Int32 = new(
        static (ref writer, value) => writer.WriteInt32(value),
        static (ref reader) => reader.ReadInt32());

    public static readonly Binary<uint> UInt32 = new(
        static (ref writer, value) => writer.WriteUInt32(value),
        static (ref reader) => reader.ReadUInt32());

    public static readonly Binary<long> Int64 = new(
        static (ref writer, value) => writer.WriteInt64(value),
        static (ref reader) => reader.ReadInt64());

    public static readonly Binary<ulong> UInt64 = new(
        static (ref writer, value) => writer.WriteUInt64(value),
        static (ref reader) => reader.ReadUInt64());

    public static readonly Binary<nint> IntPtr = new(
        static (ref writer, value) => writer.WriteInt64(value),
        static (ref reader) => (nint)reader.ReadInt64());

    public static readonly Binary<float> Single = new(
        static (ref writer, value) => writer.WriteSingle(value),
        static (ref reader) => reader.ReadSingle());

    public static readonly Binary<double> Double = new(
        static (ref writer, value) => writer.WriteDouble(value),
        static (ref reader) => reader.ReadDouble());

    public static readonly Binary<decimal> Decimal = new(
        static (ref writer, value) => writer.WriteDouble((double)value),
        static (ref reader) => (decimal)reader.ReadDouble());

    public static readonly Binary<char> Char = new(
        static (ref writer, value) => writer.WriteUInt16(value),
        static (ref reader) => (char)reader.ReadUInt16());

    public static readonly Binary<string?> String = new(
        static (ref writer, value) => writer.WriteString(value),
        static (ref reader) => reader.ReadString());

    public static readonly Binary<DateTime> DateTime = new(
        static (ref writer, value) => writer.WriteInt64(value.ToUniversalTime().Ticks),
        static (ref reader) => new(reader.ReadInt64(), DateTimeKind.Utc));

    public static readonly Binary<DateTimeOffset> DateTimeOffset = new(
        static (ref writer, value) => writer.WriteInt64(value.UtcTicks),
        static (ref reader) => new(reader.ReadInt64(), TimeSpan.Zero));

    public static Binary<TEnum> Enum<TEnum> () where TEnum : struct, Enum => new(
        static (ref writer, value) => writer.WriteInt32((int)(object)value),
        static (ref reader) => (TEnum)(object)reader.ReadInt32()
    );

    public static Binary<T?> Nullable<T> (Binary<T> inner) where T : struct => new(
        (ref writer, value) => WriteNullable(ref writer, value, inner),
        (ref reader) => ReadNullable(ref reader, inner)
    );

    public static Binary<T[]?> Array<T> (Binary<T> element) => new(
        (ref writer, value) => WriteArray(ref writer, value, element),
        (ref reader) => ReadArray(ref reader, element)
    );

    public static Binary<List<T>?> List<T> (Binary<T> element) => new(
        (ref writer, value) => WriteList(ref writer, value, value?.Count, element),
        (ref reader) => ReadList(ref reader, element)
    );

    public static Binary<IList<T>?> IList<T> (Binary<T> element) => new(
        (ref writer, value) => WriteList(ref writer, value, value?.Count, element),
        (ref reader) => ReadList(ref reader, element)
    );

    public static Binary<IReadOnlyList<T>?> IReadOnlyList<T> (Binary<T> element) => new(
        (ref writer, value) => WriteList(ref writer, value, value?.Count, element),
        (ref reader) => ReadList(ref reader, element)
    );

    public static Binary<ICollection<T>?> ICollection<T> (Binary<T> element) => new(
        (ref writer, value) => WriteList(ref writer, value, value?.Count, element),
        (ref reader) => ReadList(ref reader, element)
    );

    public static Binary<IReadOnlyCollection<T>?> IReadOnlyCollection<T> (Binary<T> element) => new(
        (ref writer, value) => WriteList(ref writer, value, value?.Count, element),
        (ref reader) => ReadList(ref reader, element)
    );

    public static Binary<Dictionary<TKey, TValue>?> Dictionary<TKey, TValue>
        (Binary<TKey> key, Binary<TValue> value) where TKey : notnull => new(
        (ref writer, map) => WriteDictionary(ref writer, map, map?.Count, key, value),
        (ref reader) => ReadDictionary(ref reader, key, value)
    );

    public static Binary<IDictionary<TKey, TValue>?> IDictionary<TKey, TValue>
        (Binary<TKey> key, Binary<TValue> value) where TKey : notnull => new(
        (ref writer, map) => WriteDictionary(ref writer, map, map?.Count, key, value),
        (ref reader) => ReadDictionary(ref reader, key, value)
    );

    public static Binary<IReadOnlyDictionary<TKey, TValue>?> IReadOnlyDictionary<TKey, TValue>
        (Binary<TKey> key, Binary<TValue> value) where TKey : notnull => new(
        (ref writer, map) => WriteDictionary(ref writer, map, map?.Count, key, value),
        (ref reader) => ReadDictionary(ref reader, key, value)
    );

    private static void WriteNullable<T> (ref Writer writer, T? value, Binary<T> inner) where T : struct
    {
        writer.WriteBool(value.HasValue);
        if (value.HasValue) inner.Write(ref writer, value.Value);
    }

    private static T? ReadNullable<T> (ref Reader reader, Binary<T> inner) where T : struct
    {
        return reader.ReadBool() ? inner.Read(ref reader) : null;
    }

    private static void WriteArray<T> (ref Writer writer, T[]? value, Binary<T> element)
    {
        if (value is null)
        {
            writer.WriteMeta(-1);
            return;
        }
        writer.WriteMeta(value.Length);
        if (RuntimeHelpers.IsReferenceOrContainsReferences<T>())
            for (int i = 0; i < value.Length; i++)
                element.Write(ref writer, value[i]);
        else
        {
            ref var source = ref Unsafe.As<T, byte>(ref MemoryMarshal.GetArrayDataReference(value));
            var bytes = MemoryMarshal.CreateReadOnlySpan(ref source, value.Length * Unsafe.SizeOf<T>());
            writer.WriteBytes(bytes);
        }
    }

    private static T[]? ReadArray<T> (ref Reader reader, Binary<T> element)
    {
        var count = reader.ReadMeta();
        if (count < 0) return null;
        var result = new T[count];
        if (RuntimeHelpers.IsReferenceOrContainsReferences<T>())
            for (int i = 0; i < result.Length; i++)
                result[i] = element.Read(ref reader)!;
        else
        {
            ref var destination = ref Unsafe.As<T, byte>(ref MemoryMarshal.GetArrayDataReference(result));
            var bytes = MemoryMarshal.CreateSpan(ref destination, result.Length * Unsafe.SizeOf<T>());
            reader.ReadBytes(bytes);
        }
        return result;
    }

    private static void WriteList<T> (ref Writer writer, IEnumerable<T>? value, int? count, Binary<T> element)
    {
        if (value is null)
        {
            writer.WriteMeta(-1);
            return;
        }
        writer.WriteMeta(count!.Value);
        foreach (var item in value)
            element.Write(ref writer, item);
    }

    private static List<T>? ReadList<T> (ref Reader reader, Binary<T> element)
    {
        var count = reader.ReadMeta();
        if (count < 0) return null;
        var result = new List<T>(count);
        for (int i = 0; i < count; i++)
            result.Add(element.Read(ref reader)!);
        return result;
    }

    private static void WriteDictionary<TKey, TValue> (ref Writer writer,
        IEnumerable<KeyValuePair<TKey, TValue>>? map,
        int? count, Binary<TKey> key, Binary<TValue> value) where TKey : notnull
    {
        if (map is null)
        {
            writer.WriteMeta(-1);
            return;
        }
        writer.WriteMeta(count!.Value);
        foreach (var pair in map)
        {
            key.Write(ref writer, pair.Key);
            value.Write(ref writer, pair.Value);
        }
    }

    private static Dictionary<TKey, TValue>? ReadDictionary<TKey, TValue> (ref Reader reader,
        Binary<TKey> key, Binary<TValue> value) where TKey : notnull
    {
        var count = reader.ReadMeta();
        if (count < 0) return null;
        var result = new Dictionary<TKey, TValue>(count);
        for (int i = 0; i < count; i++)
            result[key.Read(ref reader)!] = value.Read(ref reader)!;
        return result;
    }
}
