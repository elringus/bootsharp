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
        return inspection.StaticMembers.Select(m => m.Assembly)
            .Concat(inspection.StaticInterfaces.SelectMany(i => i.Members.Select(m => m.Assembly)))
            .Concat(inspection.InstancedInterfaces.SelectMany(i => i.Members.Select(m => m.Assembly)))
            .ToHashSet();
    }

    private HashSet<string> GetDiscoveredMembers (SolutionInspection inspection)
    {
        return inspection.StaticMembers
            .Concat(inspection.StaticInterfaces.SelectMany(i => i.Members))
            .Concat(inspection.InstancedInterfaces.SelectMany(i => i.Members))
            .Select(m => m.ToString())
            .ToHashSet();
    }
}
