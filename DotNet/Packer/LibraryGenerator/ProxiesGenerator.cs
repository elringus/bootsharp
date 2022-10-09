using System.Linq;
using static Packer.TextUtilities;

namespace Packer;

internal class ProxiesGenerator
{
    private readonly SpaceObjectBuilder objectBuilder = new();
    private readonly EnumGenerator enumGenerator;
    private readonly AssemblyInspector inspector;

    public ProxiesGenerator (AssemblyInspector inspector, NamespaceBuilder spaceBuilder)
    {
        this.inspector = inspector;
        enumGenerator = new(spaceBuilder);
    }

    public string Generate () => JoinLines(
        JoinLines(inspector.Methods.Where(m => m.Type == MethodType.Invokable).Select(EmitInvokable)),
        JoinLines(inspector.Methods.Where(m => m.Type == MethodType.Function).Select(EmitFunction)),
        JoinLines(inspector.Methods.Where(m => m.Type == MethodType.Event).Select(EmitEvent)),
        JoinLines(inspector.Types.Where(t => t.IsEnum).Select(e => enumGenerator.Generate(e, objectBuilder)))
    );

    private string EmitInvokable (Method method)
    {
        var js = $"exports.{method.Namespace}.{method.Name} = proxy.{method.Namespace}.{method.Name};";
        return objectBuilder.EnsureNamespaceObjectsDeclared(method.Namespace, js);
    }

    private string EmitFunction (Method method)
    {
        var js = JoinLines(
            $"Object.defineProperty(exports.{method.Namespace}, \"{method.Name}\", {{", JoinLines(2, true,
                "get: () => this.value,",
                $"set: value => {{ this.value = value; proxy.{method.Namespace}.{method.Name} = exports.proxy(value); }}"),
            "});"
        );
        return objectBuilder.EnsureNamespaceObjectsDeclared(method.Namespace, js);
    }

    private string EmitEvent (Method method)
    {
        var js = JoinLines(
            $"exports.{method.Namespace}.{method.Name} = {{", JoinLines(2, true,
                $"subscribe: handler => proxy.{method.Namespace}.{method.Name}.subscribe(exports.proxy(handler)),",
                $"unsubscribe: handler => proxy.{method.Namespace}.{method.Name}.unsubscribe(exports.proxy(handler))"),
            "};"
        );
        return objectBuilder.EnsureNamespaceObjectsDeclared(method.Namespace, js);
    }
}
