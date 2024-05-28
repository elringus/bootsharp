global using static Bootsharp.Publish.GlobalSerialization;
using System.Collections.Frozen;

namespace Bootsharp.Publish;

internal static class GlobalSerialization
{
    private static readonly FrozenSet<string> native = new[] {
        typeof(string).FullName!, typeof(bool).FullName!, typeof(byte).FullName!,
        typeof(char).FullName!, typeof(short).FullName!, typeof(long).FullName!,
        typeof(int).FullName!, typeof(float).FullName!, typeof(double).FullName!,
        typeof(nint).FullName!, typeof(Task).FullName!, typeof(DateTime).FullName!,
        typeof(DateTimeOffset).FullName!, typeof(Exception).FullName!
    }.ToFrozenSet();

    private static readonly FrozenSet<string> arrayNative = new[] {
        typeof(byte).FullName!, typeof(int).FullName!,
        typeof(double).FullName!, typeof(string).FullName!
    }.ToFrozenSet();

    public static string MarshalAmbiguous (ValueMeta meta, bool @return)
    {
        var typeSyntax = meta.TypeSyntax;
        var promise = typeSyntax.StartsWith("global::System.Threading.Tasks.Task<");
        if (promise) typeSyntax = typeSyntax[36..];
        var result =
            typeSyntax.StartsWith("global::System.DateTime") ? "JSType.Date" :
            typeSyntax.StartsWith("global::System.Int64") ? "JSType.BigInt" : "";
        if (result == "") return "";
        if (promise) result = $"JSType.Promise<{result}>";
        result = $"JSMarshalAs<{result}>";
        if (@return) result = $"return: {result}";
        return $"[{result}] ";
    }

    // https://learn.microsoft.com/en-us/aspnet/core/blazor/javascript-interoperability/import-export-interop
    public static bool ShouldSerialize (Type type)
    {
        if (type.IsEnum) return true;
        if (IsVoid(type)) return false;
        if (IsInstancedInteropInterface(type, out _)) return false;
        if (IsTaskWithResult(type, out var result))
            // TODO: Remove 'IsList(result)' when resolved: https://github.com/elringus/bootsharp/issues/138
            return IsList(result) || ShouldSerialize(result);
        var array = type.IsArray;
        if (array) type = type.GetElementType()!;
        if (IsNullable(type)) type = GetNullableUnderlyingType(type);
        if (array) return !arrayNative.Contains(type.FullName!);
        return !native.Contains(type.FullName!);
    }
}
