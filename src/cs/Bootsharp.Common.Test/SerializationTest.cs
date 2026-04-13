#nullable enable

using System.Collections.ObjectModel;

namespace Bootsharp.Common.Test;

public class SerializationTest
{
    private enum TestEnum { A, B }

    [Fact]
    public void BinaryDelegatesAreInvoked ()
    {
        var type = new Binary<int>(
            static (ref writer, value) => writer.WriteInt32(value + 1),
            static (ref reader) => reader.ReadInt32() - 1);
        Assert.Equal(41, Echo(type, 41));
    }

    [Fact]
    public void SerializerCanSerializeAndDeserializeValues ()
    {
        var handle = Serializer.Serialize(42, Serializer.Int32);
        Assert.Equal(42, Serializer.Deserialize(handle, Serializer.Int32));
    }

    [Fact]
    public void SerializerCanSerializeAndDeserializeNulls ()
    {
        Assert.Equal(0, Serializer.Serialize<string?>(null, Serializer.String));
        Assert.Null(Serializer.Deserialize(0, Serializer.String));
    }

    [Fact]
    public void CanEchoPrimitives ()
    {
        Assert.True(Echo(Serializer.Boolean, true));
        Assert.False(Echo(Serializer.Boolean, false));
        Assert.Equal((byte)7, Echo(Serializer.Byte, (byte)7));
        Assert.Equal((sbyte)-7, Echo(Serializer.SByte, (sbyte)-7));
        Assert.Equal((short)-1234, Echo(Serializer.Int16, (short)-1234));
        Assert.Equal((ushort)1234, Echo(Serializer.UInt16, (ushort)1234));
        Assert.Equal(-1234567, Echo(Serializer.Int32, -1234567));
        Assert.Equal(1234567u, Echo(Serializer.UInt32, 1234567u));
        Assert.Equal(-1234567890123456789L, Echo(Serializer.Int64, -1234567890123456789L));
        Assert.Equal(12345678901234567890UL, Echo(Serializer.UInt64, 12345678901234567890UL));
        Assert.Equal(123.25f, Echo(Serializer.Single, 123.25f));
        Assert.Equal(456.5d, Echo(Serializer.Double, 456.5d));
        Assert.Equal('X', Echo(Serializer.Char, 'X'));
        Assert.Equal(0x12345678, Echo(Serializer.IntPtr, 0x12345678));
        Assert.Equal(123.5m, Echo(Serializer.Decimal, 123.5m));
    }

    [Fact]
    public void CanEchoDateTime ()
    {
        var date = new DateTime(2024, 1, 2, 3, 4, 5, DateTimeKind.Utc);
        Assert.Equal(date, Echo(Serializer.DateTime, date));
        var dateOffset = new DateTimeOffset(2024, 1, 2, 3, 4, 5, TimeSpan.FromHours(3));
        var dateOffsetResult = Echo(Serializer.DateTimeOffset, dateOffset);
        Assert.Equal(dateOffset.UtcTicks, dateOffsetResult.UtcTicks);
        Assert.Equal(TimeSpan.Zero, dateOffsetResult.Offset);
    }

    [Fact]
    public void CanEchoEnums ()
    {
        Assert.Equal(TestEnum.A, Echo(Serializer.Enum<TestEnum>(), TestEnum.A));
        Assert.Equal(TestEnum.B, Echo(Serializer.Enum<TestEnum>(), TestEnum.B));
    }

    [Fact]
    public void CanEchoStrings ()
    {
        Assert.Equal("bootsharp", Echo(Serializer.String, "bootsharp"));
        Assert.Equal(string.Empty, Echo(Serializer.String, string.Empty));
        Assert.Equal(new string('a', 200), Echo(Serializer.String, new string('a', 200)));
        Assert.Null(Echo(Serializer.String, default));
    }

    [Fact]
    public void CanEchoNullable ()
    {
        Assert.Equal(42, Echo(Serializer.Nullable(Serializer.Int32), 42));
        Assert.Null(Echo(Serializer.Nullable(Serializer.Int32), default));
    }

    [Fact]
    public void CanEchoArrays ()
    {
        var ints = Enumerable.Range(-100, 200).ToArray();
        var strings = new[] { "alpha", "beta" };
        Assert.Equal(ints, Echo(Serializer.Array(Serializer.Int32), ints));
        Assert.Equal(strings, Echo(Serializer.Array(Serializer.String), strings));
        Assert.Null(Echo(Serializer.Array(Serializer.String), default));
    }

    [Fact]
    public void CanEchoLists ()
    {
        Assert.Equal([1, 2, 3], Echo(Serializer.List(Serializer.Int32), [1, 2, 3]));
        Assert.Equal([4, 5], Echo(Serializer.IList(Serializer.Int32), new List<int> { 4, 5 }));
        Assert.Equal([6, 7], Echo(Serializer.IReadOnlyList(Serializer.Int32), new ReadOnlyCollection<int>([6, 7])));
        Assert.Equal([8, 9], Echo(Serializer.ICollection(Serializer.Int32), new Collection<int>([8, 9])));
        Assert.Equal([10, 11], Echo(Serializer.IReadOnlyCollection(Serializer.Int32), new ReadOnlyCollection<int>([10, 11])));
        Assert.Null(Echo(Serializer.List(Serializer.Int32), default));
        Assert.Null(Echo(Serializer.IList(Serializer.Int32), default));
        Assert.Null(Echo(Serializer.IReadOnlyList(Serializer.Int32), default));
        Assert.Null(Echo(Serializer.ICollection(Serializer.Int32), default));
        Assert.Null(Echo(Serializer.IReadOnlyCollection(Serializer.Int32), default));
    }

    [Fact]
    public void CanEchoDictionaries ()
    {
        var d = new Dictionary<int, string?> { [1] = "one", [2] = "two" };
        IDictionary<int, string?> id = new Dictionary<int, string?> { [3] = "three", [4] = "four" };
        IReadOnlyDictionary<int, string?> ro = new Dictionary<int, string?> { [5] = "five", [6] = "six" };
        Assert.Equivalent(d, Echo(Serializer.Dictionary(Serializer.Int32, Serializer.String), d));
        Assert.Equivalent(id, Echo(Serializer.IDictionary(Serializer.Int32, Serializer.String), id));
        Assert.Equivalent(ro, Echo(Serializer.IReadOnlyDictionary(Serializer.Int32, Serializer.String), ro));
        Assert.Null(Echo(Serializer.Dictionary(Serializer.Int32, Serializer.String), default));
        Assert.Null(Echo(Serializer.IDictionary(Serializer.Int32, Serializer.String), default));
        Assert.Null(Echo(Serializer.IReadOnlyDictionary(Serializer.Int32, Serializer.String), default));
    }

    private static T? Echo<T> (Binary<T> type, T? value)
    {
        var writer = new Writer();
        type.Write(ref writer, value);
        var reader = new Reader(writer.Detach());
        try { return type.Read(ref reader); }
        finally { reader.Dispose(); }
    }
}
