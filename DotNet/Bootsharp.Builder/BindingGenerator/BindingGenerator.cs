using System.Linq;
using static Bootsharp.Builder.TextUtilities;

namespace Bootsharp.Builder;

internal sealed class BindingGenerator(NamespaceBuilder spaceBuilder, AssemblyInspector inspector)
{
    private readonly SpaceObjectBuilder objectBuilder = new();
    private readonly EnumGenerator enumGenerator = new(spaceBuilder);

    public string Generate ()
    {
        objectBuilder.Reset();
        return JoinLines(0,
            """import { invoke, invokeVoid, invokeAsync, invokeVoidAsync } from "./exports";""",
            """import { Event } from "./event";""",
            JoinLines(inspector.Methods.Where(m => m.Type == MethodType.Invokable).Select(EmitInvokable)),
            JoinLines(inspector.Methods.Where(m => m.Type == MethodType.Function).Select(EmitFunction)),
            JoinLines(inspector.Methods.Where(m => m.Type == MethodType.Event).Select(EmitEvent)),
            JoinLines(inspector.Types.Where(t => t.IsEnum).Select(e => enumGenerator.Generate(e, objectBuilder)))
        );
    }

    private string EmitInvokable (Method method)
    {
        var funcName = method.Async
            ? (method.ReturnType == "Promise<void>" ? "invokeVoidAsync" : "invokeAsync")
            : (method.ReturnType == "void" ? "invokeVoid" : "invoke");
        var funcArgs = string.Join(", ", method.Arguments.Select(a => a.Name));
        var endpoint = $"{method.Assembly}/{method.DeclaringName}/{method.Name}";
        var methodArgs = $"'{endpoint}'" + (funcArgs == "" ? "" : $", {funcArgs}");
        var body = $"{funcName}({methodArgs})";
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
        var js = $"exports.{method.Namespace}.{ToFirstLower(method.Name)} = new Event();";
        return objectBuilder.EnsureNamespaceObjectsDeclared(method.Namespace, js);
    }
}
