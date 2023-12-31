namespace Bootsharp.Publish;

internal sealed class ImportGenerator (string entryAssembly)
{
    public string Generate (AssemblyInspector inspector)
    {
        var bySpace = inspector.Methods
            .Where(m => m.Type != MethodType.Invokable)
            .GroupBy(i => i.DeclaringName).ToArray();
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

    private string GenerateSpace (string space, IReadOnlyList<Method> methods)
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

    private string GenerateFunctionAssign (Method method)
    {
        return $"""Function.Set("{BuildEndpoint(method, false)}", {method.Name});""";
    }

    private string GenerateImport (Method method)
    {
        var args = string.Join(", ", method.Arguments.Select(GenerateArg));
        var @return = method.ReturnsVoid ? "void" : (method.ShouldSerializeReturnType
            ? $"global::System.String{(method.ReturnsNullable ? "?" : "")}"
            : method.ReturnTypeSyntax);
        if (method.ShouldSerializeReturnType && method.ReturnsTaskLike)
            @return = $"global::System.Threading.Tasks.Task<{@return}>";
        var attr = $"""[System.Runtime.InteropServices.JavaScript.JSImport("{BuildEndpoint(method, true)}", "Bootsharp")]""";
        var date = MarshalAmbiguous(method.ReturnTypeSyntax, true);
        return $"{attr} {date}internal static partial {@return} {method.Name} ({args});";
    }

    private string GenerateArg (Argument arg)
    {
        var type = arg.ShouldSerialize
            ? $"global::System.String{(arg.Nullable ? "?" : "")}"
            : arg.TypeSyntax;
        return $"{MarshalAmbiguous(arg.TypeSyntax, false)}{type} {arg.Name}";
    }

    private string BuildEndpoint (Method method, bool import)
    {
        var name = char.ToLowerInvariant(method.Name[0]) + method.Name[1..];
        return $"{method.JSSpace}.{name}{(import ? "Serialized" : "")}";
    }
}
