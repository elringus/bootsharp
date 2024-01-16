namespace Bootsharp.Publish;

/// <summary>
/// Generates interop classes for interfaces specified under
/// <see cref="JSExportAttribute"/> and <see cref="JSImportAttribute"/>.
/// </summary>
internal sealed class InterfaceGenerator
{
    private readonly HashSet<string> classes = [];
    private readonly HashSet<string> registrations = [];

    public string Generate (AssemblyInspection inspection)
    {
        foreach (var inter in inspection.Interfaces)
            AddInterface(inter, inspection);
        return
            $$"""
              #nullable enable
              #pragma warning disable

              {{JoinLines(classes, 0)}}

              namespace Bootsharp.Generated
              {
                  internal static class InterfaceRegistrations
                  {
                      [System.Runtime.CompilerServices.ModuleInitializer]
                      internal static void RegisterInterfaces ()
                      {
                          {{JoinLines(registrations, 3)}}
                      }
                  }
              }
              """;
    }

    private void AddInterface (InterfaceMeta inter, AssemblyInspection inspection)
    {
        var methods = inspection.Methods.Where(m => m.Space == inter.FullName).ToArray();
        if (inter.Kind == InterfaceKind.Export) classes.Add(EmitExportClass(inter, methods));
        else classes.Add(EmitImportClass(inter, methods));
        registrations.Add(EmitRegistration(inter));
    }

    private string EmitExportClass (InterfaceMeta inter, MethodMeta[] methods) =>
        $$"""
          namespace {{inter.Namespace}}
          {
              public class {{inter.Name}}
              {
                  private static {{inter.TypeSyntax}} handler = null!;

                  public {{inter.Name}} ({{inter.TypeSyntax}} handler)
                  {
                      {{inter.Name}}.handler = handler;
                  }

                  {{JoinLines(methods.Select(EmitExportMethod), 2)}}
              }
          }
          """;

    private string EmitImportClass (InterfaceMeta inter, MethodMeta[] methods) =>
        $$"""
          namespace {{inter.Namespace}}
          {
              public class {{inter.Name}} : {{inter.TypeSyntax}}
              {
                  {{JoinLines(methods.Select(EmitImportMethod), 2)}}

                  {{JoinLines(methods.Select(m => EmitImportMethodImplementation(inter, m)), 2)}}
              }
          }
          """;

    private string EmitRegistration (InterfaceMeta inter) => inter.Kind == InterfaceKind.Import ?
        $"Interfaces.Register(typeof({inter.TypeSyntax}), new ImportInterface(new {inter.Name}()));" :
        $"Interfaces.Register(typeof({inter.Name}), new ExportInterface(typeof({inter.TypeSyntax}), handler => new {inter.Name}(handler)));";

    private string EmitExportMethod (MethodMeta method)
    {
        var sigArgs = string.Join(", ", method.Arguments.Select(a => $"{a.Value.TypeSyntax} {a.Name}"));
        var sig = $"public static {method.ReturnValue.TypeSyntax} {method.Name} ({sigArgs})";
        var args = string.Join(", ", method.Arguments.Select(a => a.Name));
        return $"[JSInvokable] {sig} => handler.{method.Name}({args});";
    }

    private string EmitImportMethod (MethodMeta method)
    {
        var attr = method.Kind == MethodKind.Function ? "JSFunction" : "JSEvent";
        var sigArgs = string.Join(", ", method.Arguments.Select(a => $"{a.Value.TypeSyntax} {a.Name}"));
        var sig = $"public static {method.ReturnValue.TypeSyntax} {method.Name} ({sigArgs})";
        var getter = EmitProxyGetter(method);
        var id = $"{method.Space}.{method.Name}";
        var args = string.Join(", ", method.Arguments.Select(a => a.Name));
        return $"[{attr}] {sig} => {getter}(\"{id}\")({args});";
    }

    private string EmitImportMethodImplementation (InterfaceMeta inter, MethodMeta method)
    {
        var sigArgs = string.Join(", ", method.Arguments.Select(a => $"{a.Value.TypeSyntax} {a.Name}"));
        var args = string.Join(", ", method.Arguments.Select(a => a.Name));
        return $"{method.ReturnValue.TypeSyntax} {inter.TypeSyntax}.{method.Name} ({sigArgs}) => {method.Name}({args});";
    }

    private string EmitProxyGetter (MethodMeta method)
    {
        var func = method.ReturnValue.Void ? "Action" : "Func";
        var syntax = method.Arguments.Select(a => a.Value.TypeSyntax).ToList();
        if (!method.ReturnValue.Void) syntax.Add(method.ReturnValue.TypeSyntax);
        if (syntax.Count > 0) func = $"{func}<{string.Join(", ", syntax)}>";
        return $"Proxies.Get<{func}>";
    }
}
