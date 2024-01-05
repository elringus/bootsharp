using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace Bootsharp.Publish;

internal sealed class InspectionReporter (TaskLoggingHelper logger)
{
    public void Report (AssemblyInspection inspection)
    {
        logger.LogMessage(MessageImportance.Normal, "Bootsharp assembly inspection result:");
        logger.LogMessage(MessageImportance.Normal, JoinLines($"Discovered {inspection.Assemblies.Length} assemblies:",
            JoinLines(inspection.Assemblies.Select(a => a.Name))));
        logger.LogMessage(MessageImportance.Normal, JoinLines($"Discovered {inspection.Methods.Length} JS methods:",
            JoinLines(inspection.Methods.Select(m => m.ToString()))));

        foreach (var warning in inspection.Warnings)
            logger.LogWarning(warning);
    }
}
