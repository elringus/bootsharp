namespace Bootsharp.Publish;

internal sealed class DeclarationGenerator (Preferences prefs)
{
    private readonly MemberDeclarationGenerator membersGenerator = new(prefs);
    private readonly TypeDeclarationGenerator typesGenerator = new(prefs);

    public string Generate (SolutionInspection inspection) => Fmt(0,
        """import type { EventBroadcaster, EventSubscriber } from "./event";""",
        typesGenerator.Generate(inspection),
        membersGenerator.Generate(inspection)
    ) + "\n";
}
