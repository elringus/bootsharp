using System.Diagnostics.CodeAnalysis;

namespace Bootsharp.Publish;

/// <summary>
/// Generates raw C-ABI bindings: <c>[UnmanagedCallersOnly]</c> exports and <c>[DllImport]</c> imports.
/// All non-primitive values cross the boundary as <c>long</c> serializer handles or <c>int</c> instance IDs.
/// </summary>
internal sealed class InteropGenerator (bool debug)
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
          using System.Runtime.InteropServices;

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
        var name = $"{srf.Id}_Broadcast{evt.Name}_Serialized";
        var args = string.Join(", ", evt.Args.Select(a => BuildParameter(a.Value, a.Name)));
        if (isIt) args = $"int {PrependIdArg(args)}";
        yield return $"""[DllImport("Bootsharp", EntryPoint = "{name}")] internal static extern void {name} ({args});""";

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
        yield return $"""[UnmanagedCallersOnly(EntryPoint = "{name}")] internal static void {name} ({args}) => {invName}({invArgs});""";
    }

    private IEnumerable<string> EmitPropertyExport (PropertyMeta prop)
    {
        if (prop.CanGet)
        {
            var name = $"{id}_Get{prop.Name}";
            var args = isIt ? "int _id" : "";
            var body = Export(prop.Get, isIt ? $"Instances.Exported<{it.Syntax}>(_id).{prop.Name}"
                : isMd ? $"{stx}.Get{prop.Name}()"
                : $"{stx}.{prop.Name}");
            yield return $"""[UnmanagedCallersOnly(EntryPoint = "{name}")] internal static {BuildValueSyntax(prop.Get)} {name} ({args}) => {body};""";
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
            yield return $"""[UnmanagedCallersOnly(EntryPoint = "{name}")] internal static void {name} ({args}) => {body};""";
        }
    }

    private IEnumerable<string> EmitPropertyImport (PropertyMeta prop)
    {
        if (prop.CanGet)
        {
            var srdName = $"{srf.Id}_Get{prop.Name}_Serialized";
            var args = isIt ? "int _id" : "";
            yield return $"""[DllImport("Bootsharp", EntryPoint = "{srdName}")] internal static extern {BuildValueSyntax(prop.Get)} {srdName} ({args});""";

            var name = $"{id}_Get{prop.Name}";
            var body = Import(prop.Get, isIt ? $"{srdName}(_id)" : $"{srdName}()");
            yield return $"public static {prop.Get.TypeSyntax} {name}({args}) => {body};";
        }
        if (prop.CanSet)
        {
            var srdName = $"{srf.Id}_Set{prop.Name}_Serialized";
            var srdArgs = BuildParameter(prop.Set, "value");
            if (isIt) srdArgs = $"int {PrependIdArg(srdArgs)}";
            yield return $"""[DllImport("Bootsharp", EntryPoint = "{srdName}")] internal static extern void {srdName} ({srdArgs});""";

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
        if (method.Async) return EmitMethodExportAsync(method);
        return EmitMethodExportSync(method);
    }

    private IEnumerable<string> EmitMethodImport (MethodMeta method)
    {
        if (method.Async) return EmitMethodImportAsync(method);
        return EmitMethodImportSync(method);
    }

    private IEnumerable<string> EmitMethodExportSync (MethodMeta method)
    {
        var name = BuildEntry(srf, method);
        var @return = BuildValueSyntax(method.Return);
        var sigArgs = string.Join(", ", method.Args.Select(a => BuildParameter(a.Value, a.Name)));
        if (isIt) sigArgs = $"int {PrependIdArg(sigArgs)}";
        var invArgs = string.Join(", ", method.Args.Select(Import));
        var invName = isIt
            ? $"Instances.Exported<{it.Syntax}>(_id).{method.Name}"
            : $"{stx}.{method.Name}";
        var body = Export(method.Return, $"{invName}({invArgs})");
        yield return $"""[UnmanagedCallersOnly(EntryPoint = "{name}")] internal static {@return} {name} ({sigArgs}) => {body};""";
    }

    private IEnumerable<string> EmitMethodImportSync (MethodMeta method)
    {
        var entry = BuildEntry(srf, method);
        var srdName = $"{entry}_Serialized";
        var @return = BuildValueSyntax(method.Return);
        var args = string.Join(", ", method.Args.Select(a => BuildParameter(a.Value, a.Name)));
        if (isIt) args = $"int {PrependIdArg(args)}";
        yield return $"""[DllImport("Bootsharp", EntryPoint = "{srdName}")] internal static extern {@return} {srdName} ({args});""";

        @return = method.Return.TypeSyntax;
        var sigArgs = string.Join(", ", method.Args.Select(a => $"{a.Value.TypeSyntax} {a.Name}"));
        if (isIt) sigArgs = $"int {PrependIdArg(sigArgs)}";
        var invArgs = string.Join(", ", method.Args.Select(Export));
        if (isIt) invArgs = PrependIdArg(invArgs);
        var body = Import(method.Return, $"{srdName}({invArgs})");
        yield return $"public static {@return} {entry} ({sigArgs}) => {body};";
    }

    private IEnumerable<string> EmitMethodExportAsync (MethodMeta method)
    {
        var entry = BuildEntry(srf, method);
        var notifyName = $"{entry}_Notify";
        var failName = $"{entry}_Fail";

        var sigArgs = string.Join(", ", method.Args.Select(a => BuildParameter(a.Value, a.Name)));
        if (isIt) sigArgs = $"int {PrependIdArg(sigArgs)}";
        sigArgs = string.IsNullOrEmpty(sigArgs) ? "int _taskId" : $"int _taskId, {sigArgs}";

        var invArgs = string.Join(", ", method.Args.Select(Import));
        var invName = isIt
            ? $"Instances.Exported<{it.Syntax}>(_id).{method.Name}"
            : $"{stx}.{method.Name}";

        var voidReturn = !IsTaskWithResult(method.Info.ReturnType, out _);
        var failBody = $"{failName}(_taskId, Serializer.Serialize({BuildFailMessage("_e")}, Serializer.String))";
        string notifyBody, completeBody;
        if (voidReturn)
        {
            notifyBody = $"{notifyName}(_taskId)";
            completeBody = $"await {invName}({invArgs});";
        }
        else
        {
            var exportExp = Export(method.Return, $"await {invName}({invArgs})");
            notifyBody = $"{notifyName}(_taskId, _result)";
            completeBody = $"var _result = {exportExp};";
        }

        yield return
            $$"""
              [UnmanagedCallersOnly(EntryPoint = "{{entry}}")] internal static void {{entry}} ({{sigArgs}})
              {
                  _ = Run();
                  async global::System.Threading.Tasks.Task Run ()
                  {
                      try
                      {
                          {{completeBody}}
                          {{notifyBody}};
                      }
                      catch (global::System.Exception _e)
                      {
                          {{failBody}};
                      }
                  }
              }
              """;

        var notifyParams = "int _taskId";
        if (!voidReturn) notifyParams = $"int _taskId, {BuildAsyncWire(method)} _result";
        yield return $"""[DllImport("Bootsharp", EntryPoint = "{notifyName}")] internal static extern void {notifyName} ({notifyParams});""";
        yield return $$"""[DllImport("Bootsharp", EntryPoint = "{{failName}}")] internal static extern void {{failName}} (int _taskId, long _message);""";
    }

    private IEnumerable<string> EmitMethodImportAsync (MethodMeta method)
    {
        var entry = BuildEntry(srf, method);
        var srdName = $"{entry}_Serialized";
        var completeName = $"{entry}_Complete";
        var failName = $"{entry}_Fail";

        var srdArgs = string.Join(", ", method.Args.Select(a => BuildParameter(a.Value, a.Name)));
        if (isIt) srdArgs = $"int {PrependIdArg(srdArgs)}";
        srdArgs = string.IsNullOrEmpty(srdArgs) ? "int _taskId" : $"int _taskId, {srdArgs}";

        yield return $"""[DllImport("Bootsharp", EntryPoint = "{srdName}")] internal static extern void {srdName} ({srdArgs});""";

        var voidReturn = !IsTaskWithResult(method.Info.ReturnType, out var resultClr);
        var innerStx = voidReturn ? "bool" : BuildAsyncInnerSyntax(method);

        if (voidReturn)
            yield return $"""[UnmanagedCallersOnly(EntryPoint = "{completeName}")] internal static void {completeName} (int _taskId) => PendingImports.Take<bool>(_taskId).SetResult(default);""";
        else
        {
            var wireStx = BuildAsyncWire(method);
            var importExp = Import(method.Return, "_result");
            yield return $"""[UnmanagedCallersOnly(EntryPoint = "{completeName}")] internal static void {completeName} (int _taskId, {wireStx} _result) => PendingImports.Take<{innerStx}>(_taskId).SetResult({importExp});""";
        }

        var tcsT = voidReturn ? "bool" : innerStx;
        yield return $"""[UnmanagedCallersOnly(EntryPoint = "{failName}")] internal static void {failName} (int _taskId, long _message) => PendingImports.Take<{tcsT}>(_taskId).SetException(new global::System.Exception(Serializer.Deserialize(_message, Serializer.String)!));""";

        var publicReturn = method.Return.TypeSyntax;
        var sigArgs = string.Join(", ", method.Args.Select(a => $"{a.Value.TypeSyntax} {a.Name}"));
        if (isIt) sigArgs = $"int {PrependIdArg(sigArgs)}";
        var invArgs = string.Join(", ", method.Args.Select(Export));
        var callArgs = string.IsNullOrEmpty(invArgs)
            ? (isIt ? "_taskId, _id" : "_taskId")
            : (isIt ? $"_taskId, _id, {invArgs}" : $"_taskId, {invArgs}");

        yield return
            $$"""
              public static {{publicReturn}} {{entry}} ({{sigArgs}})
              {
                  var _tcs = new global::System.Threading.Tasks.TaskCompletionSource<{{tcsT}}>();
                  var _taskId = PendingImports.Allocate(_tcs);
                  {{srdName}}({{callArgs}});
                  return _tcs.Task;
              }
              """;
    }

    private string BuildFailMessage (string exVar)
    {
        if (debug) return $"{exVar}.Message + \"\\n\" + {exVar}.StackTrace";
        return $"{exVar}.Message";
    }

    private string BuildAsyncWire (MethodMeta method)
    {
        var v = method.Return;
        if (v.IsSerialized) return "long";
        if (v.IsInstanced) return "int";
        return BuildAsyncInnerSyntax(method);
    }

    private string BuildAsyncInnerSyntax (MethodMeta method)
    {
        IsTaskWithResult(method.Info.ReturnType, out var inner);
        var nul = GetNullity(method.Info.ReturnParameter);
        var innerNul = nul.GenericTypeArguments.Length > 0 ? nul.GenericTypeArguments[0] : null;
        return BuildSyntax(inner!, innerNul);
    }

    private string BuildParameter (ValueMeta value, string name)
    {
        var type = BuildValueSyntax(value);
        return $"{type} {name}";
    }

    private string BuildValueSyntax (ValueMeta value)
    {
        // C-ABI requires blittable types; nullable wrappers aren't allowed.
        // Serialized values use 0 as the null sentinel handle, instance refs use 0 as null.
        if (value.IsInstanced) return "int";
        if (value.IsSerialized) return "long";
        return value.TypeSyntax;
    }
}
