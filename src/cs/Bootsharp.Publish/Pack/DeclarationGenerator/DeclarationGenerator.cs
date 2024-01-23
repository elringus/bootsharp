namespace Bootsharp.Publish;

internal sealed class DeclarationGenerator (Preferences prefs)
{
    private readonly MethodDeclarationGenerator methodsGenerator = new();
    private readonly TypeDeclarationGenerator typesGenerator = new(prefs);

    public string Generate (AssemblyInspection inspection) => JoinLines(0,
        """import type { Event } from "./event";""",
        typesGenerator.Generate(inspection.Crawled),
        methodsGenerator.Generate(inspection.Methods)
    ) + "\n";
}
