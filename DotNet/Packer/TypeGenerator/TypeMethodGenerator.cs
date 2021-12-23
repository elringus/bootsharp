using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Packer;

internal class TypeMethodGenerator
{
    private readonly StringBuilder builder = new();
    private readonly List<Method> methods = new();

    private Method method => methods[index];
    private Method prevMethod => index == 0 ? null : methods[index - 1];
    private Method nextMethod => index == methods.Count - 1 ? null : methods[index + 1];

    private int index;

    public string Generate (IEnumerable<Method> sourceMethods)
    {
        ResetState(sourceMethods);
        for (index = 0; index < methods.Count; index++)
            ProcessMethod();
        return builder.ToString();
    }

    private void ResetState (IEnumerable<Method> sourceMethods)
    {
        builder.Clear();
        methods.Clear();
        methods.AddRange(sourceMethods.OrderBy(m => m.Assembly));
    }

    private void ProcessMethod ()
    {
        if (ShouldAppendHeader()) AppendHeader();
        AppendMethod();
        if (ShouldAppendFooter()) AppendFooter();
    }

    private bool ShouldAppendHeader ()
    {
        if (prevMethod is null) return true;
        return prevMethod.Assembly != method.Assembly;
    }

    private void AppendHeader ()
    {
        if (ShouldOpenRoot()) builder.Append("\nexport declare const");
        var parts = method.Assembly.Split('.');
        var skipCount = parts.Length - GetRootDelta(prevMethod);
        foreach (var name in parts.Skip(skipCount))
            builder.Append($" {name}: {{");
    }

    private void AppendMethod ()
    {
        builder.Append($"\n    {method.Name}: (");
        builder.AppendJoin(", ", method.Arguments.Select(a => $"{a.Name}: {a.Type}"));
        builder.Append($") => {method.ReturnType},");
    }

    private bool ShouldAppendFooter ()
    {
        if (nextMethod is null) return true;
        return nextMethod.Assembly != method.Assembly;
    }

    private void AppendFooter ()
    {
        var rootDelta = GetRootDelta(nextMethod);
        builder.Append('\n').Append('}', rootDelta);
        builder.Append(ShouldCloseRoot() ? ';' : ',');
    }

    private int GetRootDelta (Method deltaMethod)
    {
        var curParts = method.Assembly.Split('.');
        var deltaParts = deltaMethod?.Assembly.Split('.') ?? Array.Empty<string>();
        for (int i = 0; i < deltaParts.Length; i++)
            if (i >= curParts.Length || curParts[i] != deltaParts[i])
                return curParts.Length - i;
        return curParts.Length;
    }

    private bool ShouldOpenRoot ()
    {
        if (prevMethod is null) return true;
        return GetAssemblyRoot(prevMethod) != GetAssemblyRoot(method);
    }

    private bool ShouldCloseRoot ()
    {
        if (nextMethod is null) return true;
        return GetAssemblyRoot(nextMethod) != GetAssemblyRoot(method);
    }

    private string GetAssemblyRoot (Method method) => method.Assembly.Split('.')[0];
}
