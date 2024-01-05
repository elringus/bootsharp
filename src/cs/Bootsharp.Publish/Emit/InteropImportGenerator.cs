namespace Bootsharp.Publish;

internal sealed class InteropImportGenerator (string entryAssembly)
{
    public string Generate (AssemblyInspection inspection)
    {
        var bySpace = inspection.Methods
            .Where(m => m.Type != MethodType.Invokable)
            .GroupBy(i => i.Space).ToArray();
        return bySpace.Length == 0 ? "" :
            $"""
             #nullable enable
             #pragma warning disable

             using System.Diagnostics.CodeAnalysis;
             using System.Runtime.CompilerServices;
             using System.Runtime.InteropServices.JavaScript;

             namespace Bootsharp.Imports;

             {JoinLines(bySpace.Select(g => GenerateSpace(g.Key, g.ToArray())), 0)}

             #pragma warning restore
             #nullable restore
             """;
    }

    private string GenerateSpace (string space, IReadOnlyList<MethodMeta> methods)
    {
        var name = space.Replace('.', '_');
        var asm = entryAssembly[..^4];
        return
            $$"""
              public partial class {{name}}
              {
                  [ModuleInitializer]
                  [DynamicDependency(DynamicallyAccessedMemberTypes.All, "Bootsharp.Imports.{{name}}", "{{asm}}")]
                  internal static void RegisterDynamicDependencies ()
                  {
                      {{JoinLines(methods.Select(GenerateFunctionAssign), 2)}}
                  }
                  {{JoinLines(methods.Select(GenerateImport))}}
              }
              """;
    }

    private string GenerateFunctionAssign (MethodMeta method)
    {
        return $"""Function.Set("{BuildEndpoint(method, false)}", {method.Name});""";
    }

    private string GenerateImport (MethodMeta method)
    {
        var args = string.Join(", ", method.Arguments.Select(GenerateArg));
        var @return = method.ReturnType.Void ? "void" : (method.ReturnType.ShouldSerialize
            ? $"global::System.String{(method.ReturnType.Nullable ? "?" : "")}"
            : method.ReturnType.Syntax);
        if (method.ReturnType.ShouldSerialize && method.ReturnType.TaskLike)
            @return = $"global::System.Threading.Tasks.Task<{@return}>";
        var attr = $"""[System.Runtime.InteropServices.JavaScript.JSImport("{BuildEndpoint(method, true)}", "Bootsharp")]""";
        var date = MarshalAmbiguous(method.ReturnType.Syntax, true);
        return $"{attr} {date}internal static partial {@return} {method.Name} ({args});";
    }

    private string GenerateArg (ArgumentMeta arg)
    {
        var type = arg.Type.ShouldSerialize
            ? $"global::System.String{(arg.Type.Nullable ? "?" : "")}"
            : arg.Type.Syntax;
        return $"{MarshalAmbiguous(arg.Type.Syntax, false)}{type} {arg.Name}";
    }

    private string BuildEndpoint (MethodMeta method, bool import)
    {
        var name = char.ToLowerInvariant(method.Name[0]) + method.Name[1..];
        return $"{method.JSSpace}.{name}{(import ? "Serialized" : "")}";
    }
}
