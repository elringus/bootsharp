using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Packer;

internal class MethodDeclarationGenerator
{
    private readonly StringBuilder builder = new();

    private Method method => methods[index];
    private Method prevMethod => index == 0 ? null : methods[index - 1];
    private Method nextMethod => index == methods.Length - 1 ? null : methods[index + 1];

    private Method[] methods;
    private int index;

    public string Generate (IEnumerable<Method> sourceMethods)
    {
        methods = sourceMethods.OrderBy(m => m.Namespace).ToArray();
        for (index = 0; index < methods.Length; index++)
            DeclareMethod();
        return builder.ToString();
    }

    private void DeclareMethod ()
    {
        if (ShouldOpenNamespace()) OpenNamespace();
        if (method.Type == MethodType.Invokable) DeclareInvokable();
        else DeclareFunction();
        if (ShouldCloseNamespace()) CloseNamespace();
    }

    private bool ShouldOpenNamespace ()
    {
        if (prevMethod is null) return true;
        return prevMethod.Namespace != method.Namespace;
    }

    private void OpenNamespace ()
    {
        var name = method.Namespace;
        builder.Append($"\nexport namespace {name} {{");
    }

    private bool ShouldCloseNamespace ()
    {
        if (nextMethod is null) return true;
        return nextMethod.Namespace != method.Namespace;
    }

    private void CloseNamespace ()
    {
        builder.Append("\n}");
    }

    private void DeclareInvokable ()
    {
        builder.Append($"\n    export function {method.Name}(");
        AppendArguments();
        builder.Append($"): {method.ReturnType};");
    }

    private void DeclareFunction ()
    {
        builder.Append($"\n    export let {method.Name}: (");
        AppendArguments();
        builder.Append($") => {method.ReturnType};");
    }

    private void AppendArguments ()
    {
        builder.AppendJoin(", ", method.Arguments.Select(a => $"{a.Name}: {a.Type}"));
    }
}
