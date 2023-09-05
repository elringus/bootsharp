namespace Bootsharp.Builder;

internal sealed class DeclarationGenerator(NamespaceBuilder spaceBuilder)
{
    private readonly MethodDeclarationGenerator methodsGenerator = new();
    private readonly TypeDeclarationGenerator typesGenerator = new(spaceBuilder);

    public string Generate (AssemblyInspector inspector) => JoinLines(0,
        typesGenerator.Generate(inspector.Types),
        methodsGenerator.Generate(inspector.Methods)
    ) + "\n";
}
