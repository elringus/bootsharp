namespace Bootsharp.Publish;

/// <summary>
/// Generates bindings to be picked by .NET's interop source generator.
/// Symmetrical to <see cref="JSModuleGenerator"/>, which generates the same but for the JS side.
/// </summary>
internal sealed class CSInteropGenerator
{
    private bool isIt => srf is InstanceMeta;
    private bool isMd => srf is ModuleMeta;

    private SurfaceMeta srf = null!;
    private string id = null!, stx = null!, key = null!;

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
            yield return $"{stx}.Bootsharp_{mem.Name} = &{id}_{mem.Endpoint};";
        foreach (var p in srf.Members.OfType<PropertyMeta>().Where(p => p.IK == InteropKind.Import))
        {
            if (p.CanGet) yield return $"{stx}.Bootsharp_Get{p.Name} = &{id}_Get{p.Name};";
            if (p.CanSet) yield return $"{stx}.Bootsharp_Set{p.Name} = &{id}_Set{p.Name};";
        }
    }

    private IEnumerable<string?> EmitMember (MemberMeta member, SurfaceMeta srf)
    {
        this.srf = srf;
        id = (srf as ProxyMeta)?.Proxy.Id ?? srf.Id;
        stx = (srf as ProxyMeta)?.Proxy.Syntax ?? srf.Syntax;
        key = srf is InstanceMeta { Proxy: SpecializedProxy sp } it
            ? (it.IK == InteropKind.Import ? sp.Import.Syntax : sp.Export.Syntax)
            : srf.Syntax;
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
        var name = $"{id}_Broadcast{evt.Name}_Serialized";
        var args = string.Join(", ", evt.Args.Select(a => BuildParameter(a.Value, a.Name)));
        if (isIt) args = $"int {PrependIdArg(args)}";
        yield return $"{attr}internal static partial void {name} ({args});";

        if (isIt) yield break; // instance event handlers are emitted by InstanceGenerator
        var handler = $"Handle_{id}_{evt.Name}";
        var sigArgs = string.Join(", ", evt.Args.Select(a => $"{a.Value.TypeSyntax} {a.Name}"));
        var invArgs = string.Join(", ", evt.Args.Select(ExportCS));
        yield return $"private static void {handler} ({sigArgs}) => {name}({invArgs});";
    }

    private IEnumerable<string> EmitEventImport (EventMeta evt)
    {
        var name = $"{id}_Invoke{evt.Name}";
        var args = string.Join(", ", evt.Args.Select(a => BuildParameter(a.Value, a.Name)));
        if (isIt) args = $"int {PrependIdArg(args)}";
        var invName = isIt ? $"(({stx})Instances.Resolve<{key}>(_id)).Invoke{evt.Name}"
            : isMd ? $"(({stx})Modules.Imports[typeof({key})].Instance).Invoke{evt.Name}"
            : $"{stx}.Bootsharp_Invoke_{evt.Name}";
        var invArgs = string.Join(", ", evt.Args.Select(ImportCS));
        yield return $"[JSExport] internal static void {name} ({args}) => {invName}({invArgs});";
    }

    private IEnumerable<string> EmitPropertyExport (PropertyMeta prop)
    {
        if (prop.CanGet)
        {
            var attr = $"[JSExport] {MarshalAmbiguous(prop.Get, true)}";
            var name = $"{id}_Get{prop.Name}";
            var args = isIt ? "int _id" : "";
            var body = ExportCS(prop.Get, isIt ? $"Instances.Exported<{key}>(_id).{prop.Name}"
                : isMd ? $"{stx}.Get{prop.Name}()"
                : $"{stx}.{prop.Name}");
            yield return $"{attr}internal static {BuildValueSyntax(prop.Get)} {name} ({args}) => {body};";
        }
        if (prop.CanSet)
        {
            var name = $"{id}_Set{prop.Name}";
            var args = BuildParameter(prop.Set, "value");
            if (isIt) args = $"int {PrependIdArg(args)}";
            var value = ImportCS(prop.Set, "value");
            var body = isIt ? $"Instances.Exported<{key}>(_id).{prop.Name} = {value}"
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
            var srdName = $"{id}_Get{prop.Name}_Serialized";
            var args = isIt ? "int _id" : "";
            yield return $"{attr}internal static partial {BuildValueSyntax(prop.Get)} {srdName} ({args});";

            var name = $"{id}_Get{prop.Name}";
            var body = ImportCS(prop.Get, isIt ? $"{srdName}(_id)" : $"{srdName}()");
            yield return $"public static {prop.Get.TypeSyntax} {name}({args}) => {body};";
        }
        if (prop.CanSet)
        {
            var attr = $"""[JSImport("{srf.JSNode}.set{prop.Name}Serialized", "{srf.JSModule}")]""";
            var srdName = $"{id}_Set{prop.Name}_Serialized";
            var srdArgs = BuildParameter(prop.Set, "value");
            if (isIt) srdArgs = $"int {PrependIdArg(srdArgs)}";
            yield return $"{attr} internal static partial void {srdName} ({srdArgs});";

            var name = $"{id}_Set{prop.Name}";
            var args = $"{prop.Set.TypeSyntax} value";
            if (isIt) args = $"int {PrependIdArg(args)}";
            var value = ExportCS(prop.Set, "value");
            var body = isIt ? $"{srdName}(_id, {value})" : $"{srdName}({value})";
            yield return $"public static void {name}({args}) => {body};";
        }
    }

    private IEnumerable<string> EmitMethodExport (MethodMeta method)
    {
        var wait = ShouldWait(method);
        var attr = $"[JSExport] {MarshalAmbiguous(method.Return, true)}";
        var name = $"{id}_{method.Endpoint}";
        var @return = BuildValueSyntax(method.Return);
        if (wait) @return = $"async global::System.Threading.Tasks.Task<{@return}>";
        var sigArgs = string.Join(", ", method.Args.Select(a => BuildParameter(a.Value, a.Name)));
        if (isIt) sigArgs = $"int {PrependIdArg(sigArgs)}";
        var invArgs = string.Join(", ", method.Args.Select(ImportCS));
        var invName = isIt ? $"Instances.Exported<{key}>(_id).{method.Name}"
            : isMd ? $"{stx}.{method.Endpoint}"
            : $"{stx}.{method.Name}";
        var body = ExportCS(method.Return, $"{(wait ? "await " : "")}{invName}({invArgs})");
        yield return $"{attr}internal static {@return} {name} ({sigArgs}) => {body};";
    }

    private IEnumerable<string> EmitMethodImport (MethodMeta method)
    {
        var attr = $"""[JSImport("{srf.JSNode}.{method.JSName}Serialized", "{srf.JSModule}")]""";
        var marshalAs = MarshalAmbiguous(method.Return, true);
        var name = $"{id}_{method.Endpoint}";
        var @return = BuildValueSyntax(method.Return);
        if (ShouldWait(method)) @return = $"global::System.Threading.Tasks.Task<{@return}>";
        var args = string.Join(", ", method.Args.Select(a => BuildParameter(a.Value, a.Name)));
        if (isIt) args = $"int {PrependIdArg(args)}";
        yield return $"{attr} {marshalAs}internal static partial {@return} {name}_Serialized ({args});";

        var wait = ShouldWait(method);
        @return = $"{(wait ? "async " : "")}{method.Return.TypeSyntax}";
        var sigArgs = string.Join(", ", method.Args.Select(a => $"{a.Value.TypeSyntax} {a.Name}"));
        if (isIt) sigArgs = $"int {PrependIdArg(sigArgs)}";
        var invArgs = string.Join(", ", method.Args.Select(ExportCS));
        if (isIt) invArgs = PrependIdArg(invArgs);
        var body = ImportCS(method.Return, $"{(wait ? "await " : "")}{name}_Serialized({invArgs})");
        yield return $"public static {@return} {name} ({sigArgs}) => {body};";
    }

    private string BuildParameter (ValueMeta value, string name)
    {
        var type = BuildValueSyntax(value);
        return $"{MarshalAmbiguous(value, false)}{type} {name}";
    }

    private string BuildValueSyntax (ValueMeta value)
    {
        if (value.IsInstanced) return "int";
        if (value.IsSerialized) return "long";
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
