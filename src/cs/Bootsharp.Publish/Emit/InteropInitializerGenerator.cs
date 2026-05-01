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

    private static string BuildEventSubscription (EventMeta evt)
    {
        var handler = $"Handle_{evt.Space.Replace('.', '_')}_{evt.Name}";
        return $"global::{evt.Space}.{evt.Name} += {handler};";
    }

    private static string BuildMethodAssignment (MethodMeta method)
    {
        var name = $"{method.Space.Replace('.', '_')}_{method.Name}";
        return $"global::{method.Space}.Bootsharp_{method.Name} = &{name};";
    }
}
