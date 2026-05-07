using System.Diagnostics.CodeAnalysis;

namespace Bootsharp.Publish;

/// <summary>
/// Generates bindings to be picked by .NET's interop source generator.
/// </summary>
internal sealed class InteropGenerator
{
    [MemberNotNullWhen(true, nameof(it))]
    private bool isIt => it != null;
    [MemberNotNullWhen(true, nameof(md))]
    private bool isMd => md != null;

    private string id = null!, space = null!;
    private InstancedMeta? it;
    private ModuleMeta? md;

    public string Generate (SolutionInspection spec) =>
        $$"""
          #nullable enable
          #pragma warning disable

          using System.Runtime.CompilerServices;
          using System.Runtime.InteropServices.JavaScript;

          namespace Bootsharp.Generated;

          public static partial class Interop
          {
              [ModuleInitializer]
              internal static unsafe void Initialize ()
              {
                  {{Fmt([
                      ..spec.Static.OfType<EventMeta>()
                          .Where(e => e.Interop == InteropKind.Export)
                          .Select(e => EmitStaticEventSubscription(e, e.Space)),
                      ..spec.Modules.SelectMany(md => md.Members.OfType<EventMeta>()
                          .Where(e => e.Interop == InteropKind.Export)
                          .Select(e => EmitStaticEventSubscription(e, md.FullName))),
                      ..spec.Static.OfType<MethodMeta>()
                          .Where(m => m.Interop == InteropKind.Import)
                          .Select(EmitStaticMethodAssignment)
                  ], 2)}}
              }

              {{Fmt(spec.Static.SelectMany(m => EmitMember(m, null, null)))}}
              {{Fmt(spec.Modules.SelectMany(md => md.Members.SelectMany(m => EmitMember(m, null, md))))}}
              {{Fmt(spec.Instanced.SelectMany(it => it.Members.SelectMany(m => EmitMember(m, it, null))))}}
          }
          """;

    private static string EmitStaticEventSubscription (EventMeta evt, string space)
    {
        var handler = $"Handle_{space.Replace('.', '_')}_{evt.Name}";
        return $"global::{space}.{evt.Name} += {handler};";
    }

    private static string EmitStaticMethodAssignment (MethodMeta method)
    {
        var name = $"{method.Space.Replace('.', '_')}_{method.Name}";
        return $"global::{method.Space}.Bootsharp_{method.Name} = &{name};";
    }

    private IEnumerable<string?> EmitMember (MemberMeta member, InstancedMeta? it, ModuleMeta? md)
    {
        this.it = it;
        this.md = md;
        space = it?.FullName ?? md?.FullName ?? member.Space;
        id = space.Replace('.', '_');
        return member switch {
            EventMeta { Interop: InteropKind.Export } e => EmitEventExport(e),
            EventMeta { Interop: InteropKind.Import } e => EmitEventImport(e),
            PropertyMeta { Interop: InteropKind.Export } p => EmitPropertyExport(p),
            PropertyMeta { Interop: InteropKind.Import } p => EmitPropertyImport(p),
            MethodMeta { Interop: InteropKind.Export } m => EmitMethodExport(m),
            _ => EmitMethodImport((MethodMeta)member)
        };
    }

    private IEnumerable<string?> EmitEventExport (EventMeta evt)
    {
        var attr = $"""[JSImport("{evt.JSSpace}.broadcast{evt.Name}Serialized", "Bootsharp")] """;
        var name = $"{evt.JSSpace.Replace('.', '_')}_Broadcast{evt.Name}_Serialized";
        var args = string.Join(", ", evt.Arguments.Select(a => BuildParameter(a.Value, a.Name)));
        if (isIt) args = $"{BuildSyntax(typeof(int))} {PrependIdArg(args)}";
        yield return $"{attr}internal static partial void {name} ({args});";

        if (isIt) yield break; // instanced export event handlers are emitted by InstanceGenerator
        var handler = $"Handle_{id}_{evt.Name}";
        var sigArgs = string.Join(", ", evt.Arguments.Select(a => $"{a.Value.TypeSyntax} {a.Name}"));
        var invArgs = string.Join(", ", evt.Arguments.Select(Export));
        yield return $"private static void {handler} ({sigArgs}) => {name}({invArgs});";
    }

    private IEnumerable<string> EmitEventImport (EventMeta evt)
    {
        var name = $"{id}_Invoke{evt.Name}";
        var args = string.Join(", ", evt.Arguments.Select(a => BuildParameter(a.Value, a.Name)));
        if (isIt) args = $"{BuildSyntax(typeof(int))} {PrependIdArg(args)}";
        var invName = isIt ? $"Instances.Import(_id, static id => new global::{it.FullName}(id)).Invoke{evt.Name}"
            : isMd ? $"((global::{md.FullName})Modules.Imports[typeof({md.Syntax})].Instance).Invoke{evt.Name}"
            : $"global::{evt.Info.DeclaringType!.FullName!.Replace('+', '.')}.Bootsharp_Invoke_{evt.Name}";
        var invArgs = string.Join(", ", evt.Arguments.Select(Import));
        yield return $"[JSExport] internal static void {name} ({args}) => {invName}({invArgs});";
    }

    private IEnumerable<string> EmitPropertyExport (PropertyMeta prop)
    {
        if (prop.CanGet)
        {
            var attr = $"[JSExport] {MarshalAmbiguous(prop.GetValue, true)}";
            var name = $"{id}_GetProperty{prop.Name}";
            var args = isIt ? $"{BuildSyntax(typeof(int))} _id" : "";
            var body = Export(prop.GetValue, isIt
                ? $"Instances.Exported<{it.Syntax}>(_id).{prop.Name}"
                : $"global::{space}.GetProperty{prop.Name}()");
            yield return $"{attr}internal static {BuildValueSyntax(prop.GetValue)} {name} ({args}) => {body};";
        }
        if (prop.CanSet)
        {
            var name = $"{id}_SetProperty{prop.Name}";
            var args = BuildParameter(prop.SetValue, "value");
            if (isIt) args = $"{BuildSyntax(typeof(int))} {PrependIdArg(args)}";
            var value = Import(prop.SetValue, "value");
            var body = isIt
                ? $"Instances.Exported<{it.Syntax}>(_id).{prop.Name} = {value}"
                : $"global::{space}.SetProperty{prop.Name}({value})";
            yield return $"[JSExport] internal static void {name} ({args}) => {body};";
        }
    }

    private IEnumerable<string> EmitPropertyImport (PropertyMeta prop)
    {
        if (prop.CanGet)
        {
            var endpoint = $"""("{prop.JSSpace}.getProperty{prop.Name}Serialized", "Bootsharp")""";
            var attr = $"[JSImport{endpoint}] {MarshalAmbiguous(prop.GetValue, true)}";
            var serdeName = $"{prop.JSSpace.Replace('.', '_')}_GetProperty{prop.Name}_Serialized";
            var args = isIt ? $"{BuildSyntax(typeof(int))} _id" : "";
            yield return $"{attr}internal static partial {BuildValueSyntax(prop.GetValue)} {serdeName} ({args});";

            var name = $"{id}_GetProperty{prop.Name}";
            var body = Import(prop.GetValue, isIt ? $"{serdeName}(_id)" : $"{serdeName}()");
            yield return $"public static {prop.GetValue.TypeSyntax} {name}({args}) => {body};";
        }
        if (prop.CanSet)
        {
            var attr = $"""[JSImport("{prop.JSSpace}.setProperty{prop.Name}Serialized", "Bootsharp")] """;
            var serdeName = $"{prop.JSSpace.Replace('.', '_')}_SetProperty{prop.Name}_Serialized";
            var serdeArgs = BuildParameter(prop.SetValue, "value");
            if (isIt) serdeArgs = $"{BuildSyntax(typeof(int))} {PrependIdArg(serdeArgs)}";
            yield return $"{attr}internal static partial void {serdeName} ({serdeArgs});";

            var name = $"{id}_SetProperty{prop.Name}";
            var args = $"{prop.SetValue.TypeSyntax} value";
            if (isIt) args = $"{BuildSyntax(typeof(int))} {PrependIdArg(args)}";
            var value = Export(prop.SetValue, "value");
            var body = isIt ? $"{serdeName}(_id, {value})" : $"{serdeName}({value})";
            yield return $"public static void {name}({args}) => {body};";
        }
    }

    private IEnumerable<string> EmitMethodExport (MethodMeta method)
    {
        var wait = ShouldWait(method);
        var attr = $"[JSExport] {MarshalAmbiguous(method.Return, true)}";
        var name = $"{id}_{method.Name}";
        var @return = BuildValueSyntax(method.Return);
        if (wait) @return = $"async global::System.Threading.Tasks.Task<{@return}>";
        var sigArgs = string.Join(", ", method.Arguments.Select(a => BuildParameter(a.Value, a.Name)));
        if (isIt) sigArgs = $"{BuildSyntax(typeof(int))} {PrependIdArg(sigArgs)}";
        var invArgs = string.Join(", ", method.Arguments.Select(Import));
        var invName = isIt
            ? $"Instances.Exported<{it.Syntax}>(_id).{method.Name}"
            : $"global::{space}.{method.Name}";
        var body = Export(method.Return, $"{(wait ? "await " : "")}{invName}({invArgs})");
        yield return $"{attr}internal static {@return} {name} ({sigArgs}) => {body};";
    }

    private IEnumerable<string> EmitMethodImport (MethodMeta method)
    {
        var marshalAs = MarshalAmbiguous(method.Return, true);
        var attr = $"""[JSImport("{method.JSSpace}.{method.JSName}Serialized", "Bootsharp")] {marshalAs}""";
        var name = $"{id}_{method.Name}";
        var @return = BuildValueSyntax(method.Return);
        if (ShouldWait(method)) @return = $"global::System.Threading.Tasks.Task<{@return}>";
        var args = string.Join(", ", method.Arguments.Select(a => BuildParameter(a.Value, a.Name)));
        if (isIt) args = $"{BuildSyntax(typeof(int))} {PrependIdArg(args)}";
        yield return $"{attr}internal static partial {@return} {name}_Serialized ({args});";

        var wait = ShouldWait(method);
        @return = $"{(wait ? "async " : "")}{method.Return.TypeSyntax}";
        var sigArgs = string.Join(", ", method.Arguments.Select(a => $"{a.Value.TypeSyntax} {a.Name}"));
        if (isIt) sigArgs = $"{BuildSyntax(typeof(int))} {PrependIdArg(sigArgs)}";
        var invArgs = string.Join(", ", method.Arguments.Select(Export));
        if (isIt) invArgs = PrependIdArg(invArgs);
        var body = Import(method.Return, $"{(wait ? "await " : "")}{name}_Serialized({invArgs})");
        yield return $"public static {@return} {name} ({sigArgs}) => {body};";
    }

    private string BuildParameter (ValueMeta value, string name)
    {
        var type = BuildValueSyntax(value);
        return $"{MarshalAmbiguous(value, false)}{type} {name}";
    }

    private string BuildValueSyntax (ValueMeta value)
    {
        var nil = value.Nullable && !value.IsSerialized ? "?" : "";
        if (value.IsInstanced) return $"global::System.Int32{nil}";
        if (value.IsSerialized) return $"global::System.Int64{nil}";
        return value.TypeSyntax;
    }

    private static string MarshalAmbiguous (ValueMeta value, bool @return)
    {
        var stx = value.TypeSyntax;
        var promise = stx.StartsWith("global::System.Threading.Tasks.Task<");
        if (promise) stx = stx[36..];

        var result = "";
        if (value.IsSerialized || stx.StartsWith("global::System.Int64")) result = "JSType.BigInt";
        else if (stx.StartsWith("global::System.DateTime")) result = "JSType.Date";
        if (result == "") return "";

        if (promise) result = $"JSType.Promise<{result}>";
        if (@return) return $"[return: JSMarshalAs<{result}>] ";
        return $"[JSMarshalAs<{result}>] ";
    }

    private bool ShouldWait (MethodMeta method)
    {
        if (!method.Async) return false;
        return method.Return.IsSerialized || method.Return.IsInstanced;
    }
}
