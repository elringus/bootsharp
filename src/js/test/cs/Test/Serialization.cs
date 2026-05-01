using System;
using System.Collections.Generic;
using Bootsharp;

namespace Test;

public record Primitives
{
    public required bool Boolean { get; init; }
    public required byte Byte { get; init; }
    public required sbyte SByte { get; init; }
    public required sbyte PositiveSByte { get; init; }
    public required short Int16 { get; init; }
    public required ushort UInt16 { get; init; }
    public required int Int32 { get; init; }
    public required uint UInt32 { get; init; }
    public required long Int64 { get; init; }
    public required ulong UInt64 { get; init; }
    public required nint IntPtr { get; init; }
    public required float Single { get; init; }
    public required double Double { get; init; }
    public required decimal Decimal { get; init; }
    public required char Char { get; init; }
    public required char EmptyChar { get; init; }
    public required char MissingChar { get; init; }
    public required string? String { get; init; }
    public required string? EmptyString { get; init; }
    public required string? LargeString { get; init; }
    public required DateTime DateTime { get; init; }
    public required DateTimeOffset DateTimeOffset { get; init; }
    public required int? NullableInt { get; init; }
    public required int? MissingInt { get; init; }
}

public readonly record struct Union
{
    public string Shared { get; }
    public ItemA? A { get; }
    public ItemB? B { get; }

    public Union (string shared, ItemA a) : this(shared) => A = a;
    public Union (string shared, ItemB b) : this(shared) => B = b;
    private Union (string shared) => Shared = shared;
}

public readonly record struct ItemA (string? String, IReadOnlyDictionary<string, int?>? Map);
public readonly record struct ItemB (string[] Strings, IReadOnlyCollection<DateTime?> Times, IReadOnlyList<int>? Ints);

public sealed record Computed
{
    public required string Id { get; init; }
    public required int Count { get; init; }
    public string Summary => $"{Id}:{Count}";
}

public static class Serialization
{
    [Export] public static Primitives?[]? EchoPrimitives (Primitives?[]? value) => value;
    [Export] public static Union?[]? EchoUnions (Union?[]? value) => value;
    [Export] public static Computed EchoComputed (Computed value) => value;
    [Export] public static Computed?[]? EchoComputedArray (Computed?[]? value) => value;
    [Export] public static byte[]? EchoBytes (byte[]? value) => value;
    [Export] public static int[]? EchoIntArray (int[]? value) => value;
    [Export] public static double[]? EchoDoubleArray (double[]? value) => value;
    [Export] public static string?[]? EchoStringArray (string?[]? value) => value;
    [Export] public static int?[]? EchoNullableIntArray (int?[]? value) => value;
    [Export] public static int[]?[]? EchoNestedIntArray (int[]?[]? value) => value;
    [Export] public static List<int>? EchoIntList (List<int>? value) => value;
    [Export] public static List<string?>? EchoStringList (List<string?>? value) => value;
    [Export] public static List<int>?[]? EchoNestedIntList (List<int>?[]? value) => value;
    [Export] public static Dictionary<string, string?>? EchoDictionary (Dictionary<string, string?>? value) => value;
    [Export] public static Dictionary<string, string?>?[]? EchoNestedDictionary (Dictionary<string, string?>?[]? value) => value;
    [Export] public static IList<int> EchoListInterface (IList<int> value) => value;
    [Export] public static IReadOnlyList<int> EchoReadOnlyList (IReadOnlyList<int> value) => value;
    [Export] public static ICollection<int> EchoCollection (ICollection<int> value) => value;
    [Export] public static IReadOnlyCollection<int> EchoReadOnlyCollection (IReadOnlyCollection<int> value) => value;
    [Export] public static IDictionary<int, int> EchoDictionaryInterface (IDictionary<int, int> value) => value;
    [Export] public static IReadOnlyDictionary<int, int> EchoReadOnlyDictionary (IReadOnlyDictionary<int, int> value) => value;
}
