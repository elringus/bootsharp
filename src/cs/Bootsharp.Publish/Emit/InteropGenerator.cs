using System.Diagnostics.CodeAnalysis;

namespace Bootsharp.Publish;

/// <summary>
/// Generates bindings to be picked by .NET's interop source generator.
/// </summary>
internal sealed class InteropGenerator
{
    private readonly HashSet<InstancedMeta> registered = [];
    private InstancedMeta? it, md;
    [MemberNotNullWhen(true, nameof(it))] private bool isIt => it != null;
    [MemberNotNullWhen(true, nameof(md))] private bool isMd => md != null;

    public string Generate (SolutionInspection spec) =>
        $$"""
          #nullable enable
          #pragma warning disable

          using System.Runtime.CompilerServices;
          using System.Runtime.InteropServices.JavaScript;

          namespace Bootsharp.Generated;

          public static partial class Interop
          {
              [JSExport] internal static void DisposeExportedInstance (int id) => Instances.DisposeExported(id);
              [JSImport("instances.disposeImported", "Bootsharp")] internal static partial void DisposeImportedInstance (int id);

              [ModuleInitializer]
              internal static unsafe void Initialize ()
              {
                  {{Fmt([
                      ..spec.Static.OfType<EventMeta>()
                          .Concat(spec.Modules.SelectMany(i => i.Members.OfType<EventMeta>()))
                          .Where(e => e.Interop == InteropKind.Export)
                          .Select(EmitEventSubscription),
                      ..spec.Static.OfType<MethodMeta>()
                          .Where(m => m.Interop == InteropKind.Import)
                          .Select(EmitMethodAssignment)
                  ], 2)}}
              }

              {{Fmt(spec.Static.SelectMany(m => EmitMember(m, null, null)))}}
              {{Fmt(spec.Modules.SelectMany(md => md.Members.SelectMany(m => EmitMember(m, null, md))))}}
              {{Fmt(spec.Instanced.SelectMany(it => it.Members.SelectMany(m => EmitMember(m, it, null))))}}
          }
          """;

    private static string EmitEventSubscription (EventMeta evt)
    {
        var handler = $"Handle_{evt.Space.Replace('.', '_')}_{evt.Name}";
        return $"global::{evt.Space}.{evt.Name} += {handler};";
    }

    private static string EmitMethodAssignment (MethodMeta method)
    {
        var name = $"{method.Space.Replace('.', '_')}_{method.Name}";
        return $"global::{method.Space}.Bootsharp_{method.Name} = &{name};";
    }

    private IEnumerable<string?> EmitMember (MemberMeta member, InstancedMeta? it, InstancedMeta? md)
    {
        this.it = it;
        this.md = md;
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

        if (isIt) yield return EmitInstanceRegistrar(it);
        if (isIt) yield break; // instanced export event handlers are emitted in the registrar
        var handler = $"Handle_{evt.Space.Replace('.', '_')}_{evt.Name}";
        var sigArgs = string.Join(", ", evt.Arguments.Select(a => $"{a.Value.TypeSyntax} {a.Name}"));
        var invArgs = string.Join(", ", evt.Arguments.Select(Serialize));
        yield return $"private static void {handler} ({sigArgs}) => {name}({invArgs});";
    }

    private IEnumerable<string> EmitEventImport (EventMeta evt)
    {
        var name = $"{evt.Space.Replace('.', '_')}_Invoke{evt.Name}";
        var args = string.Join(", ", evt.Arguments.Select(a => BuildParameter(a.Value, a.Name)));
        if (isIt) args = $"{BuildSyntax(typeof(int))} {PrependIdArg(args)}";
        var invName = isIt ? $"Instances.Import(_id, static id => new global::{it.FullName}(id)).Invoke{evt.Name}"
            : isMd ? $"((global::{evt.Space})Modules.Imports[typeof({md.Syntax})].Instance).Invoke{evt.Name}"
            : $"global::{evt.Info.DeclaringType!.FullName!.Replace('+', '.')}.Bootsharp_Invoke_{evt.Name}";
        var invArgs = string.Join(", ", evt.Arguments.Select(Deserialize));
        yield return $"[JSExport] internal static void {name} ({args}) => {invName}({invArgs});";
    }

    private IEnumerable<string> EmitPropertyExport (PropertyMeta prop)
    {
        if (prop.CanGet)
        {
            var attr = $"[JSExport] {MarshalAmbiguous(prop.GetValue, true)}";
            var name = $"{prop.Space.Replace('.', '_')}_GetProperty{prop.Name}";
            var args = isIt ? $"{BuildSyntax(typeof(int))} _id" : "";
            var body = Serialize(prop.GetValue, isIt
                ? $"Instances.Exported<{it.Syntax}>(_id).{prop.Name}"
                : $"global::{prop.Space}.GetProperty{prop.Name}()");
            yield return $"{attr}internal static {BuildValueSyntax(prop.GetValue)} {name} ({args}) => {body};";
        }
        if (prop.CanSet)
        {
            var name = $"{prop.Space.Replace('.', '_')}_SetProperty{prop.Name}";
            var args = BuildParameter(prop.SetValue, "value");
            if (isIt) args = $"{BuildSyntax(typeof(int))} {PrependIdArg(args)}";
            var value = Deserialize(prop.SetValue, "value");
            var body = isIt
                ? $"Instances.Exported<{it.Syntax}>(_id).{prop.Name} = {value}"
                : $"global::{prop.Space}.SetProperty{prop.Name}({value})";
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

            var name = $"{prop.Space.Replace('.', '_')}_GetProperty{prop.Name}";
            var body = Deserialize(prop.GetValue, isIt ? $"{serdeName}(_id)" : $"{serdeName}()");
            yield return $"public static {prop.GetValue.TypeSyntax} {name}({args}) => {body};";
        }
        if (prop.CanSet)
        {
            var attr = $"""[JSImport("{prop.JSSpace}.setProperty{prop.Name}Serialized", "Bootsharp")] """;
            var serdeName = $"{prop.JSSpace.Replace('.', '_')}_SetProperty{prop.Name}_Serialized";
            var serdeArgs = BuildParameter(prop.SetValue, "value");
            if (isIt) serdeArgs = $"{BuildSyntax(typeof(int))} {PrependIdArg(serdeArgs)}";
            yield return $"{attr}internal static partial void {serdeName} ({serdeArgs});";

            var name = $"{prop.Space.Replace('.', '_')}_SetProperty{prop.Name}";
            var args = $"{prop.SetValue.TypeSyntax} value";
            if (isIt) args = $"{BuildSyntax(typeof(int))} {PrependIdArg(args)}";
            var value = Serialize(prop.SetValue, "value");
            var body = isIt ? $"{serdeName}(_id, {value})" : $"{serdeName}({value})";
            yield return $"public static void {name}({args}) => {body};";
        }
    }

    private IEnumerable<string> EmitMethodExport (MethodMeta method)
    {
        var wait = ShouldWait(method);
        var attr = $"[JSExport] {MarshalAmbiguous(method.Return, true)}";
        var name = $"{method.Space.Replace('.', '_')}_{method.Name}";
        var @return = BuildValueSyntax(method.Return);
        if (wait) @return = $"async global::System.Threading.Tasks.Task<{@return}>";
        var sigArgs = string.Join(", ", method.Arguments.Select(a => BuildParameter(a.Value, a.Name)));
        if (isIt) sigArgs = $"{BuildSyntax(typeof(int))} {PrependIdArg(sigArgs)}";
        var invArgs = string.Join(", ", method.Arguments.Select(Deserialize));
        var invName = isIt
            ? $"Instances.Exported<{it.Syntax}>(_id).{method.Name}"
            : $"global::{method.Space}.{method.Name}";
        var body = Serialize(method.Return, $"{(wait ? "await " : "")}{invName}({invArgs})");
        yield return $"{attr}internal static {@return} {name} ({sigArgs}) => {body};";
    }

    private IEnumerable<string> EmitMethodImport (MethodMeta method)
    {
        var marshalAs = MarshalAmbiguous(method.Return, true);
        var attr = $"""[JSImport("{method.JSSpace}.{method.JSName}Serialized", "Bootsharp")] {marshalAs}""";
        var name = $"{method.Space.Replace('.', '_')}_{method.Name}";
        var @return = BuildValueSyntax(method.Return);
        if (ShouldWait(method)) @return = $"global::System.Threading.Tasks.Task<{@return}>";
        var args = string.Join(", ", method.Arguments.Select(a => BuildParameter(a.Value, a.Name)));
        if (isIt) args = $"{BuildSyntax(typeof(int))} {PrependIdArg(args)}";
        yield return $"{attr}internal static partial {@return} {name}_Serialized ({args});";

        var wait = ShouldWait(method);
        @return = $"{(wait ? "async " : "")}{method.Return.TypeSyntax}";
        var sigArgs = string.Join(", ", method.Arguments.Select(a => $"{a.Value.TypeSyntax} {a.Name}"));
        if (isIt) sigArgs = $"{BuildSyntax(typeof(int))} {PrependIdArg(sigArgs)}";
        var invArgs = string.Join(", ", method.Arguments.Select(Serialize));
        if (isIt) invArgs = PrependIdArg(invArgs);
        var body = Deserialize(method.Return, $"{(wait ? "await " : "")}{name}_Serialized({invArgs})");
        yield return $"public static {@return} {name} ({sigArgs}) => {body};";
    }

    private string? EmitInstanceRegistrar (InstancedMeta it)
    {
        if (!registered.Add(it)) return null;
        var events = it.Members.OfType<EventMeta>().ToArray();
        return
            $$"""
              private static int Register ({{it.Syntax}} instance) => Instances.Export(instance, static (_id, instance) => {
                  {{Fmt(events.Select(e => $"instance.{e.Name} += Handle{e.Name};"))}}
                  return () => {
                      {{Fmt(events.Select(e => $"instance.{e.Name} -= Handle{e.Name};"), 2)}}
                  };

                  {{Fmt(events.Select(e => {
                      var args = string.Join(", ", e.Arguments.Select(a => $"{a.Value.TypeSyntax} {a.Name}"));
                      var invArgs = PrependIdArg(string.Join(", ", e.Arguments.Select(Serialize)));
                      var name = $"{e.JSSpace.Replace('.', '_')}_Broadcast{e.Name}_Serialized";
                      return $"void Handle{e.Name} ({args}) => {name}({invArgs});";
                  }))}}
              });
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
        if (value.IsInstanced) return RegisterInstance(value.Instanced, exp);
        if (Serialized(value, out var id)) return $"Serializer.Serialize({exp}, {id})";
        return exp;
    }

    private string Deserialize (ArgumentMeta arg) => Deserialize(arg.Value, arg.Name);
    private string Deserialize (ValueMeta value, string exp)
    {
        if (value.Instanced is { } it)
        {
            if (it.Interop == InteropKind.Export) return $"Instances.Exported<{it.Syntax}>({exp})";
            return $"Instances.Import({exp}, static id => new global::{it.FullName}(id))";
        }
        if (Serialized(value, out var id)) return $"Serializer.Deserialize({exp}, {id})";
        return exp;
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

    private static bool Serialized (ValueMeta meta, [NotNullWhen(true)] out string? id)
    {
        if (!meta.IsSerialized) id = null;
        else id = $"SerializerContext.{meta.Serialized.Id}";
        return id != null;
    }

    private string RegisterInstance (InstancedMeta it, string exp)
    {
        if (it.Interop == InteropKind.Import) return $"((global::{it.FullName}){exp})._id";
        if (it.Members.OfType<EventMeta>().Any()) return $"Register({exp})";
        return $"Instances.Export({exp})";
    }

    private bool ShouldWait (MethodMeta method)
    {
        if (!method.Async) return false;
        return method.Return.IsSerialized || method.Return.IsInstanced;
    }
}
