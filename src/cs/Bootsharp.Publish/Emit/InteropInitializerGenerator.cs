namespace Bootsharp.Publish;

internal sealed class InteropInitializerGenerator
{
    public string Generate (IEnumerable<MethodMeta> methods)
    {
        var interop = methods.Where(m => m.Kind is MethodKind.Function or MethodKind.Event)
            .OrderBy(BuildProxyName).ToArray();
        if (interop.Length == 0) return "";
        return $$"""
                 {{JoinLines(interop.Select(BuildAccessor))}}

                     [ModuleInitializer]
                     internal static unsafe void Initialize ()
                     {
                         {{JoinLines(interop.Select(BuildAssignment), 2)}}
                     }
                 """;
    }

    private static string BuildAccessor (MethodMeta method)
    {
        var proxy = BuildProxyName(method);
        var ptrType = BuildPointerType(method);
        var target = BuildTargetName(method);
        var accessor = BuildAccessorName(method);
        return $"""
                [UnsafeAccessor(UnsafeAccessorKind.StaticField, Name = "{proxy}")]
                private static extern unsafe ref {ptrType} {accessor} ([UnsafeAccessorType("{target}")] object? _);
                """;
    }

    private static string BuildAssignment (MethodMeta method)
    {
        var proxy = BuildProxyName(method);
        return $"{BuildAccessorName(method)}(default) = &{proxy};";
    }

    private static string BuildPointerType (MethodMeta method)
    {
        var args = method.Arguments.Select(a => a.Value.TypeSyntax).ToList();
        args.Add(method.ReturnValue.TypeSyntax);
        return $"delegate* managed<{string.Join(", ", args)}>";
    }

    private static string BuildAccessorName (MethodMeta method)
    {
        return $"Get_{BuildProxyName(method)}";
    }

    private static string BuildProxyName (MethodMeta method)
    {
        return string.Concat($"Proxy_{method.Space}_{method.Name}"
            .Select(c => char.IsLetterOrDigit(c) || c == '_' ? c : '_'));
    }

    private static string BuildTargetName (MethodMeta method)
    {
        return $"{method.Space}, {method.Assembly}";
    }
}
