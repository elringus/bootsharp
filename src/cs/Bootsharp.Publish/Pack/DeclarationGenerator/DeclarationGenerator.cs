namespace Bootsharp.Publish;

internal sealed class DeclarationGenerator (Preferences prefs)
{
    private readonly MethodDeclarationGenerator methodsGenerator = new();
    private readonly TypeDeclarationGenerator typesGenerator = new(prefs);

    public string Generate (AssemblyInspection inspection) => JoinLines(0,
        """import type { Event } from "./event";""",
        typesGenerator.Generate(GetTypesToGenerateDeclarationsFor(inspection)),
        methodsGenerator.Generate(GetMethodsToGenerateDeclarationsFor(inspection))
    ) + "\n";

    private IEnumerable<Type> GetTypesToGenerateDeclarationsFor (AssemblyInspection inspection)
    {
        return inspection.Crawled.Where(t => !t.Namespace?.StartsWith("Bootsharp.Generated") ?? true);
    }

    private IEnumerable<MethodMeta> GetMethodsToGenerateDeclarationsFor (AssemblyInspection inspection)
    {
        return inspection.Methods.Where(m => !m.Space.StartsWith("Bootsharp.Generated"));
    }
}
