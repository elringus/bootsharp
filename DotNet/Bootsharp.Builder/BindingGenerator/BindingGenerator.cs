using System;
using System.Linq;
using System.Text;
using static Bootsharp.Builder.TextUtilities;

namespace Bootsharp.Builder;

internal sealed class BindingGenerator(NamespaceBuilder spaceBuilder)
{
    private readonly StringBuilder builder = new();

    private Binding binding => bindings[index];
    private Binding? prevBinding => index == 0 ? null : bindings[index - 1];
    private Binding? nextBinding => index == bindings.Length - 1 ? null : bindings[index + 1];

    private Binding[] bindings = null!;
    private int index, level;

    public string Generate (AssemblyInspector inspector)
    {
        bindings = inspector.Methods
            .Select(m => new Binding(m, null, m.Namespace))
            .Concat(inspector.Types.Where(t => t.IsEnum)
                .Select(t => new Binding(null, t, spaceBuilder.Build(t))))
            .OrderBy(m => m.Namespace).ToArray();
        if (bindings.Length == 0) return "";
        EmitImports();
        for (index = 0; index < bindings.Length; index++)
            EmitBinding();
        return builder.ToString();
    }

    private void EmitImports ()
    {
        builder.Append("import { invoke, invokeVoid, invokeAsync, invokeVoidAsync } from './exports';\n");
        builder.Append("import { Event } from './event';\n\n");
    }

    private void EmitBinding ()
    {
        if (ShouldOpenNamespace()) OpenNamespace();
        if (binding.Method != null) EmitMethod(binding.Method);
        else EmitEnum(binding.Enum!);
        if (ShouldCloseNamespace()) CloseNamespace();
    }

    private bool ShouldOpenNamespace ()
    {
        if (prevBinding is null) return true;
        return prevBinding.Namespace != binding.Namespace;
    }

    private void OpenNamespace ()
    {
        level = binding.Namespace.Count(c => c == '.');
        var slack = prevBinding is null ? 0 :
            binding.Namespace.IndexOf(prevBinding.Namespace, StringComparison.Ordinal) + 1;
        var parts = binding.Namespace[slack..].Split('.');
        for (var i = 0; i < parts.Length; i++)
            if (slack == 0 && i == 0) builder.Append($"export const {parts[i]} = {{\n");
            else builder.Append($"{Pad(i)}{parts[i]}: {{\n");
    }

    private bool ShouldCloseNamespace ()
    {
        if (nextBinding is null) return true;
        return nextBinding.Namespace != binding.Namespace;
    }

    private void CloseNamespace ()
    {
        var target = nextBinding is null ? 0 : nextBinding.Namespace.Count(c => c == '.');
        for (; level >= target; level--)
            if (level == 0) builder.Append("};\n");
            else builder.Append($"{Pad(level)}}},\n");
    }

    private void EmitMethod (Method method)
    {
        if (method.Type == MethodType.Invokable) EmitInvokable(method);
        else if (method.Type == MethodType.Function) EmitFunction(method);
        else EmitEvent(method);
    }

    private void EmitInvokable (Method method)
    {
        var funcName = method.Async
            ? (method.ReturnType == "Promise<void>" ? "invokeVoidAsync" : "invokeAsync")
            : (method.ReturnType == "void" ? "invokeVoid" : "invoke");
        var funcArgs = string.Join(", ", method.Arguments.Select(a => a.Name));
        var endpoint = $"{method.Assembly}/{method.DeclaringName}/{method.Name}";
        var methodArgs = $"'{endpoint}'" + (funcArgs == "" ? "" : $", {funcArgs}");
        var body = $"{funcName}({methodArgs})";
        builder.Append($"{Pad(level + 1)}{ToFirstLower(method.Name)}: ({funcArgs}) => {body},\n");
    }

    private void EmitFunction (Method method)
    {
        builder.Append($"{Pad(level + 1)}{ToFirstLower(method.Name)}: undefined,\n");
    }

    private void EmitEvent (Method method)
    {
        builder.Append($"{Pad(level + 1)}{ToFirstLower(method.Name)}: new Event(),\n");
    }

    private void EmitEnum (Type @enum)
    {
        var values = Enum.GetNames(@enum);
        var fields = string.Join(", ", values.Select(v => $"{v}: \"{v}\""));
        builder.Append($"{Pad(level + 1)}{@enum.Name}: {{ {fields} }},\n");
    }

    private string Pad (int level) => new(' ', level * 4);
}
