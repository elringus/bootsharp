namespace Bootsharp.Publish;

internal sealed class InteropInitializerGenerator
{
    public string Generate (SolutionInspection inspection)
    {
        var events = inspection.StaticMembers.OfType<EventMeta>()
            .Concat(inspection.StaticInterfaces.SelectMany(i => i.Members.OfType<EventMeta>()))
            .Where(e => e.Interop == InteropKind.Export).ToArray();
        var methods = inspection.StaticMembers.OfType<MethodMeta>()
            .Where(m => m.Interop == InteropKind.Import).ToArray();
        if (methods.Length == 0 && events.Length == 0) return "";
        return $$"""
                     {{Fmt(methods.Select(BuildMethodAccessor))}}

                     [ModuleInitializer]
                     internal static unsafe void Initialize ()
                     {
                         {{Fmt(2, [
                             ..events.Select(BuildEventSubscription),
                             ..methods.Select(BuildMethodAssignment)
                         ])}}
                     }
                 """;
    }

    private static string BuildMethodAccessor (MethodMeta method)
    {
        var name = $"{method.Space.Replace('.', '_')}_{method.Name}";
        var argType = string.Join(", ", [..method.Arguments.Select(a => a.Value.TypeSyntax), method.Value.TypeSyntax]);
        var ptrType = $"delegate* managed<{argType}>";
        var accessor = $"""[UnsafeAccessorType("{method.Space}, {method.Assembly}")]""";
        return $"""
                [UnsafeAccessor(UnsafeAccessorKind.StaticField, Name = "Bootsharp_{method.Name}")]
                private static extern unsafe ref {ptrType} Access_{name} ({accessor} object? _);
                """;
    }

    private static string BuildEventSubscription (EventMeta evt)
    {
        var handler = $"Handle_{evt.Space.Replace('.', '_')}_{evt.Name}";
        return $"global::{evt.Space}.{evt.Name} += {handler};";
    }

    private static string BuildMethodAssignment (MethodMeta method)
    {
        var name = $"{method.Space.Replace('.', '_')}_{method.Name}";
        return $"Access_{name}(default) = &{name};";
    }
}
