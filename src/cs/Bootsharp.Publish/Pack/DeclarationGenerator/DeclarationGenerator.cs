namespace Bootsharp.Publish;

internal sealed class DeclarationGenerator (Preferences prefs)
{
    private readonly MethodDeclarationGenerator methodsGenerator = new();
    private readonly TypeDeclarationGenerator typesGenerator = new(prefs);

    public string Generate (SolutionInspection inspection) => JoinLines(0,
        """import type { Event } from "./event";""",
        typesGenerator.Generate(inspection),
        methodsGenerator.Generate(inspection)
    ) + "\n";
}
