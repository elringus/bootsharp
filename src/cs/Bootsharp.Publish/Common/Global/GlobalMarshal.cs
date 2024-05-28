global using static Bootsharp.Publish.GlobalMarshal;
using System.Collections.Frozen;
using System.Reflection;

namespace Bootsharp.Publish;

internal static class GlobalMarshal
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
        var promise = meta.TypeSyntax.StartsWith("global::System.Threading.Tasks.Task<");
        if (promise) typeSyntax = meta.TypeSyntax[36..];
        var result = "";
        if (meta.Marshaled) result = "JSType.Any";
        else if (typeSyntax.StartsWith("global::System.DateTime")) result = "JSType.Date";
        else if (typeSyntax.StartsWith("global::System.Int64")) result = "JSType.BigInt";
        if (result == "") return "";
        if (promise) result = $"JSType.Promise<{result}>";
        result = $"JSMarshalAs<{result}>";
        if (@return) result = $"return: {result}";
        return $"[{result}] ";
    }

    // https://learn.microsoft.com/en-us/aspnet/core/blazor/javascript-interoperability/import-export-interop
    public static bool ShouldMarshal (Type type)
    {
        if (type.IsEnum) return true;
        if (IsVoid(type)) return false;
        if (IsInstancedInteropInterface(type, out _)) return false;
        if (IsTaskWithResult(type, out var result))
            // TODO: Remove 'IsList(result)' when resolved: https://github.com/elringus/bootsharp/issues/138
            return IsList(result) || ShouldMarshal(result);
        var array = type.IsArray;
        if (array) type = type.GetElementType()!;
        if (IsNullable(type)) type = GetNullableUnderlyingType(type);
        if (array) return !arrayNative.Contains(type.FullName!);
        return !native.Contains(type.FullName!);
    }

    public static string GetMarshalId (Type type) => BuildSyntax(type)
        .Replace('.', '_').Replace('+', '_')
        .Replace('<', '_').Replace(">", "").Replace(',', '_')
        .Replace("[", "_Array").Replace("]", "")
        .Replace("?", "")
        .Replace("global::", "").Replace(" ", "");

    public static PropertyInfo[] GetMarshaledProperties (Type type)
    {
        // Even though 'MetadataToken' is not guaranteed to be stable,
        // we use it under single shared compilation unit and only
        // at build time, hence the order is expected to be stable.
        return type.GetProperties(BindingFlags.Instance | BindingFlags.Public)
            .Where(p => p.CanWrite).OrderBy(p => p.MetadataToken).ToArray();
    }

    // TODO: Remove once solved https://github.com/elringus/bootsharp/issues/138.
    public static bool ShouldMarshalPassThrough (Type type) =>
        type.IsArray && !ShouldMarshal(type.GetElementType()!);
}
