using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace Bootsharp.Publish;

internal sealed class InspectionReporter (TaskLoggingHelper logger)
{
    public void Report (SolutionInspection inspection)
    {
        logger.LogMessage(MessageImportance.Normal, "Bootsharp assembly inspection result:");
        logger.LogMessage(MessageImportance.Normal, JoinLines("Discovered assemblies:",
            JoinLines(GetDiscoveredAssemblies(inspection))));
        logger.LogMessage(MessageImportance.Normal, JoinLines("Discovered interop methods:",
            JoinLines(GetDiscoveredMethods(inspection))));
        foreach (var warning in inspection.Warnings)
            logger.LogWarning(warning);
    }

    private HashSet<string> GetDiscoveredAssemblies (SolutionInspection inspection)
    {
        return inspection.StaticMethods
            .Concat(inspection.StaticInterfaces.SelectMany(i => i.Methods))
            .Select(m => m.Assembly)
            .ToHashSet();
    }

    private HashSet<string> GetDiscoveredMethods (SolutionInspection inspection)
    {
        return inspection.StaticMethods
            .Concat(inspection.StaticInterfaces.SelectMany(i => i.Methods))
            .Concat(inspection.InstancedInterfaces.SelectMany(i => i.Methods))
            .Select(m => m.ToString())
            .ToHashSet();
    }
}
