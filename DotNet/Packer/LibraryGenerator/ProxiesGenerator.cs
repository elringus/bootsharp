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
        const string id = "dotnetEventHandlerId";
        var path = $"{method.Namespace}.{method.Name}";
        var js = JoinLines(
            $"exports.{path} = {{", JoinLines(2, true,
                $"broadcast: proxy.{path}.broadcast,",
                "subscribe: handler => {", JoinLines(3, true,
                    $"const id = handler.hasOwnProperty('{id}') ? handler.{id} : (handler.{id} = crypto.randomUUID());",
                    $"return proxy.{path}.subscribeById(id, exports.proxy(handler));"),
                "},",
                "unsubscribe: handler => {", JoinLines(3, true,
                    $"if (handler.hasOwnProperty('{id}')) return proxy.{path}.unsubscribeById(handler.{id});",
                    "console.warn(`Failed to unsubscribe event handler: handler is not subscribed. Handler: ${handler}`);"),
                "},"),
            "};"
        );
        return objectBuilder.EnsureNamespaceObjectsDeclared(method.Namespace, js);
    }
}
