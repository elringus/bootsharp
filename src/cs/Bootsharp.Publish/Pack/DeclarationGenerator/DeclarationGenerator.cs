namespace Bootsharp.Publish;

internal sealed class DeclarationGenerator (NamespaceBuilder spaceBuilder)
{
    private readonly MethodDeclarationGenerator methodsGenerator = new();
    private readonly TypeDeclarationGenerator typesGenerator = new(spaceBuilder);

    public string Generate (AssemblyInspection inspection) => JoinLines(0,
        """import type { Event } from "./event";""",
        typesGenerator.Generate(inspection.Crawled),
        methodsGenerator.Generate(inspection.Methods)
    ) + "\n";
}
