using System.Diagnostics.CodeAnalysis;

namespace Bootsharp.Publish;

/// <summary>
/// Generates bindings to be picked by .NET's interop source generator.
/// </summary>
internal sealed class InteropGenerator
{
    private readonly HashSet<InterfaceMeta> registered = [];
    private IReadOnlyCollection<InterfaceMeta> instanced = [];

    public string Generate (SolutionInspection inspection)
    {
        instanced = inspection.InstancedInterfaces;
        return
            $$"""
              #nullable enable
              #pragma warning disable

              using System.Runtime.CompilerServices;
              using System.Runtime.InteropServices.JavaScript;

              namespace Bootsharp.Generated;

              public static partial class Interop
              {
                  [System.Runtime.InteropServices.JavaScript.JSExport] internal static void DisposeExportedInstance (int id) => Instances.DisposeExported(id);
                  [System.Runtime.InteropServices.JavaScript.JSImport("disposeImported", "Bootsharp")] internal static partial void DisposeImportedInstance (int id);

                  {{new InteropInitializerGenerator().Generate(inspection)}}

                  {{Fmt(inspection.StaticMembers.SelectMany(EmitMember))}}
                  {{Fmt(inspection.StaticInterfaces.SelectMany(i => i.Members.SelectMany(EmitMember)))}}
                  {{Fmt(inspection.InstancedInterfaces.SelectMany(i => i.Members.SelectMany(EmitMember)))}}
              }
              """;
    }

    private IEnumerable<string?> EmitMember (MemberMeta member) => member switch {
        EventMeta { Interop: InteropKind.Export } e => EmitEventExport(e),
        EventMeta { Interop: InteropKind.Import } e => EmitEventImport(e),
        PropertyMeta { Interop: InteropKind.Export } p => EmitPropertyExport(p),
        PropertyMeta { Interop: InteropKind.Import } p => EmitPropertyImport(p),
        MethodMeta { Interop: InteropKind.Export } m => EmitMethodExport(m),
        _ => EmitMethodImport((MethodMeta)member)
    };

    private IEnumerable<string?> EmitEventExport (EventMeta evt)
    {
        var instanced = TryInstanced(evt, out var instance);
        var endpoint = $"""("{evt.JSSpace}.broadcast{evt.Name}Serialized", "Bootsharp")""";
        var attr = $"[System.Runtime.InteropServices.JavaScript.JSImport{endpoint}] ";
        var name = $"{evt.JSSpace.Replace('.', '_')}_Broadcast{evt.Name}_Serialized";
        var args = string.Join(", ", evt.Arguments.Select(a => BuildParameter(a.Value, a.Name)));
        if (instanced) args = $"{BuildSyntax(typeof(int))} {PrependIdArg(args)}";
        yield return $"{attr}internal static partial void {name} ({args});";

        if (instanced) yield return EmitInstanceRegistrar(instance!);
        if (instanced) yield break; // instanced export event handlers are emitted in the registrar
        var handler = $"Handle_{evt.Space.Replace('.', '_')}_{evt.Name}";
        var sigArgs = string.Join(", ", evt.Arguments.Select(a => $"{a.Value.TypeSyntax} {a.Name}"));
        var invArgs = string.Join(", ", evt.Arguments.Select(Serialize));
        yield return $"private static void {handler} ({sigArgs}) => {name}({invArgs});";
    }

    private IEnumerable<string> EmitEventImport (EventMeta evt)
    {
        var instanced = TryInstanced(evt, out _);
        var attr = "[System.Runtime.InteropServices.JavaScript.JSExport] ";
        var name = $"{evt.Space.Replace('.', '_')}_Invoke{evt.Name}";
        var args = string.Join(", ", evt.Arguments.Select(a => BuildParameter(a.Value, a.Name)));
        if (instanced) args = $"{BuildSyntax(typeof(int))} {PrependIdArg(args)}";
        var invName = evt.Info.DeclaringType is { IsInterface: true } it
            ? instanced
                ? $"((global::{BuildInterfaceImpl(it, InteropKind.Import).full})Instances.GetImported(_id)).Invoke{evt.Name}"
                : $"((global::{evt.Space})Interfaces.Imports[typeof({BuildSyntax(it)})].Instance).Invoke{evt.Name}"
            : $"global::{evt.Info.DeclaringType!.FullName!.Replace('+', '.')}.Bootsharp_Invoke_{evt.Name}";
        var invArgs = string.Join(", ", evt.Arguments.Select(Deserialize));
        yield return $"{attr}internal static void {name} ({args}) => {invName}({invArgs});";
    }

    private IEnumerable<string> EmitPropertyExport (PropertyMeta prop)
    {
        var instanced = TryInstanced(prop, out var instance);
        if (prop.CanGet)
        {
            var marshalAs = MarshalAmbiguous(prop.Value, true);
            var attr = $"[System.Runtime.InteropServices.JavaScript.JSExport] {marshalAs}";
            var name = $"{prop.Space.Replace('.', '_')}_GetProperty{prop.Name}";
            var args = instanced ? $"{BuildSyntax(typeof(int))} _id" : "";
            var body = Serialize(prop.Value, instanced
                ? $"(({instance!.TypeSyntax})Instances.GetExported(_id)).{prop.Name}"
                : $"global::{prop.Space}.GetProperty{prop.Name}()");
            yield return $"{attr}internal static {BuildValueSyntax(prop.Value)} {name} ({args}) => {body};";
        }
        if (prop.CanSet)
        {
            var attr = "[System.Runtime.InteropServices.JavaScript.JSExport] ";
            var name = $"{prop.Space.Replace('.', '_')}_SetProperty{prop.Name}";
            var args = BuildParameter(prop.Value, "value");
            if (instanced) args = $"{BuildSyntax(typeof(int))} {PrependIdArg(args)}";
            var value = Deserialize(prop.Value, "value");
            var body = instanced
                ? $"(({instance!.TypeSyntax})Instances.GetExported(_id)).{prop.Name} = {value}"
                : $"global::{prop.Space}.SetProperty{prop.Name}({value})";
            yield return $"{attr}internal static void {name} ({args}) => {body};";
        }
    }

    private IEnumerable<string> EmitPropertyImport (PropertyMeta prop)
    {
        var instanced = TryInstanced(prop, out _);
        if (prop.CanGet)
        {
            var endpoint = $"""("{prop.JSSpace}.getProperty{prop.Name}Serialized", "Bootsharp")""";
            var marshalAs = MarshalAmbiguous(prop.Value, true);
            var attr = $"[System.Runtime.InteropServices.JavaScript.JSImport{endpoint}] {marshalAs}";
            var serdeName = $"{prop.JSSpace.Replace('.', '_')}_GetProperty{prop.Name}_Serialized";
            var args = instanced ? $"{BuildSyntax(typeof(int))} _id" : "";
            yield return $"{attr}internal static partial {BuildValueSyntax(prop.Value)} {serdeName} ({args});";

            var name = $"{prop.Space.Replace('.', '_')}_GetProperty{prop.Name}";
            var body = Deserialize(prop.Value, instanced ? $"{serdeName}(_id)" : $"{serdeName}()");
            yield return $"public static {prop.Value.TypeSyntax} {name}({args}) => {body};";
        }
        if (prop.CanSet)
        {
            var endpoint = $"""("{prop.JSSpace}.setProperty{prop.Name}Serialized", "Bootsharp")""";
            var attr = $"[System.Runtime.InteropServices.JavaScript.JSImport{endpoint}] ";
            var serdeName = $"{prop.JSSpace.Replace('.', '_')}_SetProperty{prop.Name}_Serialized";
            var serdeArgs = BuildParameter(prop.Value, "value");
            if (instanced) serdeArgs = $"{BuildSyntax(typeof(int))} {PrependIdArg(serdeArgs)}";
            yield return $"{attr}internal static partial void {serdeName} ({serdeArgs});";

            var name = $"{prop.Space.Replace('.', '_')}_SetProperty{prop.Name}";
            var args = $"{prop.Value.TypeSyntax} value";
            if (instanced) args = $"{BuildSyntax(typeof(int))} {PrependIdArg(args)}";
            var value = Serialize(prop.Value, "value");
            var body = instanced ? $"{serdeName}(_id, {value})" : $"{serdeName}({value})";
            yield return $"public static void {name}({args}) => {body};";
        }
    }

    private IEnumerable<string> EmitMethodExport (MethodMeta method)
    {
        var instanced = TryInstanced(method, out var instance);
        var wait = ShouldWait(method);
        var marshalAs = MarshalAmbiguous(method.Value, true);
        var attr = $"[System.Runtime.InteropServices.JavaScript.JSExport] {marshalAs}";
        var name = $"{method.Space.Replace('.', '_')}_{method.Name}";
        var @return = BuildValueSyntax(method.Value);
        if (wait) @return = $"async global::System.Threading.Tasks.Task<{@return}>";
        var sigArgs = string.Join(", ", method.Arguments.Select(a => BuildParameter(a.Value, a.Name)));
        if (instanced) sigArgs = $"{BuildSyntax(typeof(int))} {PrependIdArg(sigArgs)}";
        var invArgs = string.Join(", ", method.Arguments.Select(Deserialize));
        var invName = instanced
            ? $"(({instance!.TypeSyntax})Instances.GetExported(_id)).{method.Name}"
            : $"global::{method.Space}.{method.Name}";
        var body = Serialize(method.Value, $"{(wait ? "await " : "")}{invName}({invArgs})");
        yield return $"{attr}internal static {@return} {name} ({sigArgs}) => {body};";
    }

    private IEnumerable<string> EmitMethodImport (MethodMeta method)
    {
        var instanced = TryInstanced(method, out _);
        var marshalAs = MarshalAmbiguous(method.Value, true);
        var endpoint = $"""("{method.JSSpace}.{method.JSName}Serialized", "Bootsharp")""";
        var attr = $"[System.Runtime.InteropServices.JavaScript.JSImport{endpoint}] {marshalAs}";
        var name = $"{method.Space.Replace('.', '_')}_{method.Name}";
        var @return = BuildValueSyntax(method.Value);
        if (ShouldWait(method)) @return = $"global::System.Threading.Tasks.Task<{@return}>";
        var args = string.Join(", ", method.Arguments.Select(a => BuildParameter(a.Value, a.Name)));
        if (instanced) args = $"{BuildSyntax(typeof(int))} {PrependIdArg(args)}";
        yield return $"{attr}internal static partial {@return} {name}_Serialized ({args});";

        var wait = ShouldWait(method);
        @return = $"{(wait ? "async " : "")}{method.Value.TypeSyntax}";
        var sigArgs = string.Join(", ", method.Arguments.Select(a => $"{a.Value.TypeSyntax} {a.Name}"));
        if (instanced) sigArgs = $"{BuildSyntax(typeof(int))} {PrependIdArg(sigArgs)}";
        var invArgs = string.Join(", ", method.Arguments.Select(Serialize));
        if (instanced) invArgs = PrependIdArg(invArgs);
        var body = Deserialize(method.Value, $"{(wait ? "await " : "")}{name}_Serialized({invArgs})");
        yield return $"public static {@return} {name} ({sigArgs}) => {body};";
    }

    private string? EmitInstanceRegistrar (InterfaceMeta instance)
    {
        if (!registered.Add(instance)) return null;
        var events = instance.Members.OfType<EventMeta>().ToArray();
        return
            $$"""
              private static int Register ({{instance.TypeSyntax}} instance)
              {
                  if (Instances.TryGetExportedId(instance, out var _id)) return _id;
                  {{Fmt(events.Select(e => $"instance.{e.Name} += Handle{e.Name};"))}}
                  return _id = Instances.RegisterExported(instance, () => {
                      {{Fmt(events.Select(e => $"instance.{e.Name} -= Handle{e.Name};"), 2)}}
                  });

                  {{Fmt(events.Select(e => {
                      var args = string.Join(", ", e.Arguments.Select(a => $"{a.Value.TypeSyntax} {a.Name}"));
                      var invArgs = PrependIdArg(string.Join(", ", e.Arguments.Select(Serialize)));
                      var name = $"{e.JSSpace.Replace('.', '_')}_Broadcast{e.Name}_Serialized";
                      return $"void Handle{e.Name} ({args}) => {name}({invArgs});";
                  }))}}
              }
              """;
    }

    private string BuildParameter (ValueMeta value, string name)
    {
        var type = BuildValueSyntax(value);
        return $"{MarshalAmbiguous(value, false)}{type} {name}";
    }

    private string Serialize (ArgumentMeta arg) => Serialize(arg.Value, arg.Name);
    private string Serialize (ValueMeta value, string exp)
    {
        if (value.IsInstance) return RegisterInstance(value, exp);
        if (Serialized(value, out var id)) return $"Serializer.Serialize({exp}, {id})";
        return exp;
    }

    private string Deserialize (ArgumentMeta arg) => Deserialize(arg.Value, arg.Name);
    private string Deserialize (ValueMeta value, string exp)
    {
        if (value.InstanceType is { } it)
        {
            var instance = instanced.First(i => i.Type == it);
            return instance.Interop == InteropKind.Export
                ? $"({BuildSyntax(it)})Instances.GetExported({exp})"
                : $"new global::{BuildInterfaceImpl(it, InteropKind.Import).full}({exp})";
        }
        if (Serialized(value, out var id)) return $"Serializer.Deserialize({exp}, {id})";
        return exp;
    }

    private string BuildValueSyntax (ValueMeta value)
    {
        var nil = value.Nullable && !value.IsSerialized ? "?" : "";
        if (value.IsInstance) return $"global::System.Int32{nil}";
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

    private static bool Serialized (ValueMeta meta, [NotNullWhen(true)] out string? id)
    {
        if (!meta.IsSerialized) id = null;
        else id = $"SerializerContext.{meta.Serialized.Id}";
        return id != null;
    }

    private string RegisterInstance (ValueMeta value, string exp)
    {
        var it = instanced.First(i => i.Type == value.InstanceType);
        if (it.Interop == InteropKind.Import) return $"((global::{it.FullName}){exp})._id";
        if (it.Members.OfType<EventMeta>().Any()) return $"Register({exp})";
        return $"Instances.RegisterExported({exp})";
    }

    private bool TryInstanced (MemberMeta member, [NotNullWhen(true)] out InterfaceMeta? instance)
    {
        instance = instanced.FirstOrDefault(i => i.Members.Contains(member));
        return instance is not null;
    }

    private bool ShouldWait (MethodMeta method)
    {
        if (!method.Async) return false;
        return method.Value.IsSerialized || method.Value.IsInstance;
    }
}
