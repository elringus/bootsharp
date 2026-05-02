using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace Bootsharp.Publish;

internal sealed class InspectionReporter (TaskLoggingHelper logger)
{
    public void Report (SolutionInspection inspection)
    {
        logger.LogMessage(MessageImportance.Normal, "Bootsharp assembly inspection result:");
        logger.LogMessage(MessageImportance.Normal, Fmt("Discovered assemblies:",
            Fmt(GetDiscoveredAssemblies(inspection))));
        logger.LogMessage(MessageImportance.Normal, Fmt("Discovered interop members:",
            Fmt(GetDiscoveredMembers(inspection))));
        foreach (var warning in inspection.Warnings)
            logger.LogWarning(warning);
    }

    private HashSet<string> GetDiscoveredAssemblies (SolutionInspection inspection)
    {
        return inspection.Static.Select(m => m.Assembly)
            .Concat(inspection.Modules.SelectMany(i => i.Members.Select(m => m.Assembly)))
            .Concat(inspection.Instanced.SelectMany(i => i.Members.Select(m => m.Assembly)))
            .ToHashSet();
    }

    private HashSet<string> GetDiscoveredMembers (SolutionInspection inspection)
    {
        return inspection.Static
            .Concat(inspection.Modules.SelectMany(i => i.Members))
            .Concat(inspection.Instanced.SelectMany(i => i.Members))
            .Select(m => m.ToString())
            .ToHashSet();
    }
}
