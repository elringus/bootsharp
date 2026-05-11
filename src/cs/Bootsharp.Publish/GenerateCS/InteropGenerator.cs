using System.Diagnostics.CodeAnalysis;

namespace Bootsharp.Publish;

/// <summary>
/// Generates bindings to be picked by .NET's interop source generator.
/// </summary>
internal sealed class InteropGenerator
{
    [MemberNotNullWhen(true, nameof(it))]
    private bool isIt => srf is InstanceMeta;
    [MemberNotNullWhen(true, nameof(md))]
    private bool isMd => srf is ModuleMeta;

    private string id = null!, stx = null!;
    private SurfaceMeta srf = null!;
    private InstanceMeta? it => srf as InstanceMeta;
    private ModuleMeta? md => srf as ModuleMeta;

    public string Generate (IReadOnlyCollection<SurfaceMeta> srf) =>
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
                  {{Fmt(srf.SelectMany(EmitInitializers), 2)}}
              }

              {{Fmt(srf.SelectMany(s => s.Members.SelectMany(m => EmitMember(m, s))))}}
          }
          """;

    private static IEnumerable<string> EmitInitializers (SurfaceMeta srf)
    {
        if (srf is InstanceMeta) yield break;
        var id = (srf as ProxyMeta)?.Proxy.Id ?? srf.Id;
        var stx = (srf as ProxyMeta)?.Proxy.Syntax ?? srf.Syntax;
        foreach (var evt in srf.Members.OfType<EventMeta>().Where(e => e.IK == InteropKind.Export))
            yield return $"{stx}.{evt.Name} += Handle_{id}_{evt.Name};";
        if (srf is not StaticMeta) yield break;
        foreach (var mem in srf.Members.OfType<MethodMeta>().Where(m => m.IK == InteropKind.Import))
            yield return $"{srf.Syntax}.Bootsharp_{mem.Name} = &{srf.Id}_{mem.Name};";
        foreach (var p in srf.Members.OfType<PropertyMeta>().Where(p => p.IK == InteropKind.Import))
        {
            if (p.CanGet) yield return $"{srf.Syntax}.Bootsharp_Get{p.Name} = &{srf.Id}_Get{p.Name};";
            if (p.CanSet) yield return $"{srf.Syntax}.Bootsharp_Set{p.Name} = &{srf.Id}_Set{p.Name};";
        }
    }

    private IEnumerable<string?> EmitMember (MemberMeta member, SurfaceMeta srf)
    {
        this.srf = srf;
        id = (srf as ProxyMeta)?.Proxy.Id ?? srf.Id;
        stx = (srf as ProxyMeta)?.Proxy.Syntax ?? srf.Syntax;
        return member switch {
            EventMeta { IK: InteropKind.Export } e => EmitEventExport(e),
            EventMeta { IK: InteropKind.Import } e => EmitEventImport(e),
            PropertyMeta { IK: InteropKind.Export } p => EmitPropertyExport(p),
            PropertyMeta { IK: InteropKind.Import } p => EmitPropertyImport(p),
            MethodMeta { IK: InteropKind.Export } m => EmitMethodExport(m),
            _ => EmitMethodImport((MethodMeta)member)
        };
    }

    private IEnumerable<string?> EmitEventExport (EventMeta evt)
    {
        var attr = $"""[JSImport("{srf.JSNode}.broadcast{evt.Name}Serialized", "{srf.JSModule}")] """;
        var name = $"{srf.Id}_Broadcast{evt.Name}_Serialized";
        var args = string.Join(", ", evt.Args.Select(a => BuildParameter(a.Value, a.Name)));
        if (isIt) args = $"int {PrependIdArg(args)}";
        yield return $"{attr}internal static partial void {name} ({args});";

        if (isIt) yield break; // instance event handlers are emitted by InstanceGenerator
        var handler = $"Handle_{id}_{evt.Name}";
        var sigArgs = string.Join(", ", evt.Args.Select(a => $"{a.Value.TypeSyntax} {a.Name}"));
        var invArgs = string.Join(", ", evt.Args.Select(Export));
        yield return $"private static void {handler} ({sigArgs}) => {name}({invArgs});";
    }

    private IEnumerable<string> EmitEventImport (EventMeta evt)
    {
        var name = $"{id}_Invoke{evt.Name}";
        var args = string.Join(", ", evt.Args.Select(a => BuildParameter(a.Value, a.Name)));
        if (isIt) args = $"int {PrependIdArg(args)}";
        var invName = isIt ? $"(({it.Proxy.Syntax})Instances.Resolve<{it.Syntax}>(_id)).Invoke{evt.Name}"
            : isMd ? $"(({md.Proxy.Syntax})Modules.Imports[typeof({md.Syntax})].Instance).Invoke{evt.Name}"
            : $"{srf.Syntax}.Bootsharp_Invoke_{evt.Name}";
        var invArgs = string.Join(", ", evt.Args.Select(Import));
        yield return $"[JSExport] internal static void {name} ({args}) => {invName}({invArgs});";
    }

    private IEnumerable<string> EmitPropertyExport (PropertyMeta prop)
    {
        if (prop.CanGet)
        {
            var attr = $"[JSExport] {MarshalAmbiguous(prop.Get, true)}";
            var name = $"{id}_Get{prop.Name}";
            var args = isIt ? "int _id" : "";
            var body = Export(prop.Get, isIt ? $"Instances.Exported<{it.Syntax}>(_id).{prop.Name}"
                : isMd ? $"{stx}.Get{prop.Name}()"
                : $"{stx}.{prop.Name}");
            yield return $"{attr}internal static {BuildValueSyntax(prop.Get)} {name} ({args}) => {body};";
        }
        if (prop.CanSet)
        {
            var name = $"{id}_Set{prop.Name}";
            var args = BuildParameter(prop.Set, "value");
            if (isIt) args = $"int {PrependIdArg(args)}";
            var value = Import(prop.Set, "value");
            var body = isIt ? $"Instances.Exported<{it.Syntax}>(_id).{prop.Name} = {value}"
                : isMd ? $"{stx}.Set{prop.Name}({value})"
                : $"{stx}.{prop.Name} = {value}";
            yield return $"[JSExport] internal static void {name} ({args}) => {body};";
        }
    }

    private IEnumerable<string> EmitPropertyImport (PropertyMeta prop)
    {
        if (prop.CanGet)
        {
            var endpoint = $"""("{srf.JSNode}.get{prop.Name}Serialized", "{srf.JSModule}")""";
            var attr = $"[JSImport{endpoint}] {MarshalAmbiguous(prop.Get, true)}";
            var srdName = $"{srf.Id}_Get{prop.Name}_Serialized";
            var args = isIt ? "int _id" : "";
            yield return $"{attr}internal static partial {BuildValueSyntax(prop.Get)} {srdName} ({args});";

            var name = $"{id}_Get{prop.Name}";
            var body = Import(prop.Get, isIt ? $"{srdName}(_id)" : $"{srdName}()");
            yield return $"public static {prop.Get.TypeSyntax} {name}({args}) => {body};";
        }
        if (prop.CanSet)
        {
            var attr = $"""[JSImport("{srf.JSNode}.set{prop.Name}Serialized", "{srf.JSModule}")]""";
            var srdName = $"{srf.Id}_Set{prop.Name}_Serialized";
            var srdArgs = BuildParameter(prop.Set, "value");
            if (isIt) srdArgs = $"int {PrependIdArg(srdArgs)}";
            yield return $"{attr} internal static partial void {srdName} ({srdArgs});";

            var name = $"{id}_Set{prop.Name}";
            var args = $"{prop.Set.TypeSyntax} value";
            if (isIt) args = $"int {PrependIdArg(args)}";
            var value = Export(prop.Set, "value");
            var body = isIt ? $"{srdName}(_id, {value})" : $"{srdName}({value})";
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
        var sigArgs = string.Join(", ", method.Args.Select(a => BuildParameter(a.Value, a.Name)));
        if (isIt) sigArgs = $"int {PrependIdArg(sigArgs)}";
        var invArgs = string.Join(", ", method.Args.Select(Import));
        var invName = isIt
            ? $"Instances.Exported<{it.Syntax}>(_id).{method.Name}"
            : $"{stx}.{method.Name}";
        var body = Export(method.Return, $"{(wait ? "await " : "")}{invName}({invArgs})");
        yield return $"{attr}internal static {@return} {name} ({sigArgs}) => {body};";
    }

    private IEnumerable<string> EmitMethodImport (MethodMeta method)
    {
        var attr = $"""[JSImport("{srf.JSNode}.{method.JSName}Serialized", "{srf.JSModule}")]""";
        var marshalAs = MarshalAmbiguous(method.Return, true);
        var name = $"{id}_{method.Name}";
        var @return = BuildValueSyntax(method.Return);
        if (ShouldWait(method)) @return = $"global::System.Threading.Tasks.Task<{@return}>";
        var args = string.Join(", ", method.Args.Select(a => BuildParameter(a.Value, a.Name)));
        if (isIt) args = $"int {PrependIdArg(args)}";
        yield return $"{attr} {marshalAs}internal static partial {@return} {name}_Serialized ({args});";

        var wait = ShouldWait(method);
        @return = $"{(wait ? "async " : "")}{method.Return.TypeSyntax}";
        var sigArgs = string.Join(", ", method.Args.Select(a => $"{a.Value.TypeSyntax} {a.Name}"));
        if (isIt) sigArgs = $"int {PrependIdArg(sigArgs)}";
        var invArgs = string.Join(", ", method.Args.Select(Export));
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
        if (value.IsInstanced) return $"int{nil}";
        if (value.IsSerialized) return $"long{nil}";
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
