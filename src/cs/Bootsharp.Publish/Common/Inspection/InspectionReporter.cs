using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace Bootsharp.Publish;

internal sealed class InspectionReporter (TaskLoggingHelper logger)
{
    public void Report (SolutionInspection spec)
    {
        logger.LogMessage(MessageImportance.Normal, "Bootsharp assembly inspection result:");
        logger.LogMessage(MessageImportance.Normal, Fmt("Discovered assemblies:",
            Fmt(GetDiscoveredAssemblies(spec))));
        logger.LogMessage(MessageImportance.Normal, Fmt("Discovered interop members:",
            Fmt(GetDiscoveredMembers(spec))));
        foreach (var warning in spec.Warnings)
            logger.LogWarning(warning);
    }

    private HashSet<string> GetDiscoveredAssemblies (SolutionInspection spec)
    {
        return spec.Types.OfType<SurfaceMeta>().SelectMany(s => s.Members)
            .Select(m => m.Info.DeclaringType!.Assembly.GetName().Name!)
            .ToHashSet();
    }

    private HashSet<string> GetDiscoveredMembers (SolutionInspection spec)
    {
        return spec.Types.OfType<SurfaceMeta>().SelectMany(s => s.Members)
            .Select(m => m.ToString())
            .ToHashSet();
    }
}
