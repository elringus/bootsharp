﻿using System.Diagnostics.CodeAnalysis;

namespace Bootsharp.Publish;

/// <summary>
/// Generates bindings to be picked by .NET's interop source generator.
/// </summary>
internal sealed class InteropGenerator
{
    private readonly HashSet<string> proxies = [];
    private readonly HashSet<string> methods = [];
    private IReadOnlyCollection<InterfaceMeta> instanced = [];

    public string Generate (SolutionInspection inspection)
    {
        instanced = inspection.InstancedInterfaces;
        var @static = inspection.StaticMethods
            .Concat(inspection.StaticInterfaces.SelectMany(i => i.Methods))
            .Concat(inspection.InstancedInterfaces.SelectMany(i => i.Methods));
        foreach (var meta in @static) // @formatter:off
            if (meta.Kind == MethodKind.Invokable) AddExportMethod(meta);
            else { AddProxy(meta); AddImportMethod(meta); } // @formatter:on
        return
            $$"""
              #nullable enable
              #pragma warning disable

              using System.Runtime.InteropServices.JavaScript;
              using static Bootsharp.Serializer;

              namespace Bootsharp.Generated;

              public static partial class Interop
              {
                  [System.Runtime.CompilerServices.ModuleInitializer]
                  internal static void RegisterProxies ()
                  {
                      {{JoinLines(proxies, 2)}}
                  }

                  [System.Runtime.InteropServices.JavaScript.JSExport] internal static void DisposeExportedInstance (int id) => global::Bootsharp.Instances.Dispose(id);
                  [System.Runtime.InteropServices.JavaScript.JSImport("disposeInstance", "Bootsharp")] internal static partial void DisposeImportedInstance (int id);

                  {{JoinLines(methods)}}
              }
              """;
    }

    private void AddExportMethod (MethodMeta inv)
    {
        var instanced = TryInstanced(inv, out var instance);
        var marshalAs = MarshalAmbiguous(inv.ReturnValue, true);
        var wait = ShouldWait(inv.ReturnValue);
        var attr = $"[System.Runtime.InteropServices.JavaScript.JSExport] {marshalAs}";
        methods.Add($"{attr}internal static {BuildSignature()} => {BuildBody()};");

        string BuildSignature ()
        {
            var args = string.Join(", ", inv.Arguments.Select(BuildSignatureArg));
            if (instanced) args = args = PrependInstanceIdArgTypeAndName(args);
            var @return = BuildReturnValue(inv.ReturnValue);
            var signature = $"{@return} {BuildMethodName(inv)} ({args})";
            if (wait) signature = $"async {signature}";
            return signature;
        }

        string BuildBody ()
        {
            var args = string.Join(", ", inv.Arguments.Select(BuildBodyArg));
            var body = instanced
                ? $"(({instance!.TypeSyntax})global::Bootsharp.Instances.Get(_id)).{inv.Name}({args})"
                : $"global::{inv.Space}.{inv.Name}({args})";
            if (wait) body = $"await {body}";
            if (inv.ReturnValue.Instance) body = $"global::Bootsharp.Instances.Register({body})";
            else if (inv.ReturnValue.Serialized) body = $"Serialize({body}, {BuildSerdeType(inv.ReturnValue)})";
            return body;
        }

        string BuildBodyArg (ArgumentMeta arg)
        {
            if (arg.Value.Instance)
            {
                var (_, _, full) = BuildInteropInterfaceImplementationName(arg.Value.InstanceType, InterfaceKind.Import);
                return $"new global::{full}({arg.Name})";
            }
            if (arg.Value.Serialized) return $"Deserialize<{arg.Value.TypeSyntax}>({arg.Name})";
            return arg.Name;
        }
    }

    private void AddProxy (MethodMeta method)
    {
        var instanced = TryInstanced(method, out _);
        var id = $"{method.Space}.{method.Name}";
        var args = string.Join(", ", method.Arguments.Select(arg => $"{arg.Value.TypeSyntax} {arg.Name}"));
        if (instanced) args = args = PrependInstanceIdArgTypeAndName(args);
        var wait = ShouldWait(method.ReturnValue);
        var async = wait ? "async " : "";
        proxies.Add($"""Proxies.Set("{id}", {async}({args}) => {BuildBody()});""");

        string BuildBody ()
        {
            var args = string.Join(", ", method.Arguments.Select(BuildBodyArg));
            if (instanced) args = PrependInstanceIdArgName(args);
            var body = $"{BuildMethodName(method)}({args})";
            if (wait) body = $"await {body}";
            if (method.ReturnValue.Instance)
            {
                var (_, _, full) = BuildInteropInterfaceImplementationName(method.ReturnValue.InstanceType, InterfaceKind.Import);
                return $"({BuildSyntax(method.ReturnValue.InstanceType)})new global::{full}({body})";
            }
            if (!method.ReturnValue.Serialized) return body;
            return $"Deserialize<{StripTaskSyntax(method.ReturnValue)}>({body})";
        }

        string BuildBodyArg (ArgumentMeta arg)
        {
            if (arg.Value.Instance) return $"global::Bootsharp.Instances.Register({arg.Name})";
            if (arg.Value.Serialized) return $"Serialize({arg.Name}, {BuildSerdeType(arg.Value)})";
            return arg.Name;
        }
    }

    private void AddImportMethod (MethodMeta method)
    {
        var args = string.Join(", ", method.Arguments.Select(BuildSignatureArg));
        if (TryInstanced(method, out _)) args = PrependInstanceIdArgTypeAndName(args);
        var @return = BuildReturnValue(method.ReturnValue);
        var endpoint = $"{method.JSSpace}.{method.JSName}Serialized";
        var attr = $"""[System.Runtime.InteropServices.JavaScript.JSImport("{endpoint}", "Bootsharp")]""";
        var marsh = MarshalAmbiguous(method.ReturnValue, true);
        methods.Add($"{attr} {marsh}internal static partial {@return} {BuildMethodName(method)} ({args});");
    }

    private string BuildValueType (ValueMeta value)
    {
        if (value.Void) return "void";
        var nil = value.Nullable ? "?" : "";
        if (value.Instance) return $"global::System.Int32{nil}";
        if (value.Serialized) return $"global::System.String{nil}";
        return value.TypeSyntax;
    }

    private string BuildSignatureArg (ArgumentMeta arg)
    {
        var type = BuildValueType(arg.Value);
        return $"{MarshalAmbiguous(arg.Value, false)}{type} {arg.Name}";
    }

    private string BuildReturnValue (ValueMeta value)
    {
        var syntax = ShouldMarshalAsAny(value.Type) ? "object" : BuildValueType(value);
        if (ShouldWait(value)) syntax = $"global::System.Threading.Tasks.Task<{syntax}>";
        return syntax;
    }

    private string BuildMethodName (MethodMeta method)
    {
        return $"{method.Space.Replace('.', '_')}_{method.Name}";
    }

    private bool TryInstanced (MethodMeta method, [NotNullWhen(true)] out InterfaceMeta? instance)
    {
        instance = instanced.FirstOrDefault(i => i.Methods.Contains(method));
        return instance is not null;
    }

    private bool ShouldWait (ValueMeta value)
    {
        return value.Async && (value.Serialized || ShouldMarshalAsAny(value.Type) || value.Instance);
    }

    private string BuildSerdeType (ValueMeta value)
    {
        return $"typeof({StripTaskSyntax(value)})".Replace("?", "");
    }

    private string StripTaskSyntax (ValueMeta value)
    {
        return value.Async
            ? value.TypeSyntax[36..^1]
            : value.TypeSyntax;
    }
}
