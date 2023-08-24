using System.Linq;
using static Bootsharp.Packer.TextUtilities;

namespace Bootsharp.Packer;

internal sealed class BindingsGenerator(AssemblyInspector inspector, NamespaceBuilder spaceBuilder)
{
    private readonly SpaceObjectBuilder objectBuilder = new();
    private readonly EnumGenerator enumGenerator = new(spaceBuilder);

    public string Generate ()
    {
        objectBuilder.Reset();
        return JoinLines(
            JoinLines(inspector.Methods.Where(m => m.Type == MethodType.Invokable).Select(EmitInvokable)),
            JoinLines(inspector.Methods.Where(m => m.Type == MethodType.Function).Select(EmitFunction)),
            JoinLines(inspector.Methods.Where(m => m.Type == MethodType.Event).Select(EmitEvent)),
            JoinLines(inspector.Types.Where(t => t.IsEnum).Select(e => enumGenerator.Generate(e, objectBuilder)))
        );
    }

    private string EmitInvokable (Method method)
    {
        var funcArgs = string.Join(", ", method.Arguments.Select(a => a.Name));
        var methodArgs = $"'{method.Assembly}', '{method.Name}'" + (funcArgs == "" ? "" : $", {funcArgs}");
        var invoke = method.Async ? "invokeAsync" : "invoke";
        var body = $"exports.{invoke}({methodArgs})";
        var js = $"exports.{method.Namespace}.{ToFirstLower(method.Name)} = ({funcArgs}) => {body};";
        return objectBuilder.EnsureNamespaceObjectsDeclared(method.Namespace, js);
    }

    private string EmitFunction (Method method)
    {
        var js = $"exports.{method.Namespace}.{ToFirstLower(method.Name)} = undefined;";
        return objectBuilder.EnsureNamespaceObjectsDeclared(method.Namespace, js);
    }

    private string EmitEvent (Method method)
    {
        var js = $"exports.{method.Namespace}.{ToFirstLower(method.Name)} = new exports.Event();";
        return objectBuilder.EnsureNamespaceObjectsDeclared(method.Namespace, js);
    }
}
