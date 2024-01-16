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

    private void AddInterface (InterfaceMeta i, AssemblyInspection inspection)
    {
        var methods = inspection.Methods.Where(m => m.Space == i.FullName).ToArray();
        if (i.Kind == InterfaceKind.Export) classes.Add(EmitExportClass(i, methods));
        else classes.Add(EmitImportClass(i, methods));
        registrations.Add(EmitRegistration(i));
    }

    private string EmitExportClass (InterfaceMeta i, MethodMeta[] methods) =>
        $$"""
          namespace {{i.Namespace}}
          {
              public class {{i.Name}}
              {
                  private static {{i.TypeSyntax}} handler = null!;

                  public {{i.Name}} ({{i.TypeSyntax}} handler)
                  {
                      {{i.Name}}.handler = handler;
                  }

                  {{JoinLines(methods.Select(EmitExportMethod), 2)}}
              }
          }
          """;

    private string EmitImportClass (InterfaceMeta i, MethodMeta[] methods) =>
        $$"""
          namespace {{i.Namespace}}
          {
              public class {{i.Name}} : {{i.TypeSyntax}}
              {
                  {{JoinLines(methods.Select(EmitImportMethod), 2)}}

                  {{JoinLines(methods.Select(m => EmitImportMethodImplementation(i, m)), 2)}}
              }
          }
          """;

    private string EmitRegistration (InterfaceMeta i) => i.Kind == InterfaceKind.Import ?
        $"Interfaces.Register(typeof({i.TypeSyntax}), new ImportInterface(new {i.Name}()));" :
        $"Interfaces.Register(typeof({i.Name}), new ExportInterface(typeof({i.TypeSyntax}), handler => new {i.Name}(handler)));";

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

    private string EmitImportMethodImplementation (InterfaceMeta i, MethodMeta method)
    {
        var sigArgs = string.Join(", ", method.Arguments.Select(a => $"{a.Value.TypeSyntax} {a.Name}"));
        var args = string.Join(", ", method.Arguments.Select(a => a.Name));
        return $"{method.ReturnValue.TypeSyntax} {i.TypeSyntax}.{method.Name} ({sigArgs}) => {method.Name}({args});";
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
