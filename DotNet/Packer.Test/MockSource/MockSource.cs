namespace Packer.Test;

public abstract class MockSource
{
    public abstract string[] GetExpectedInitLines (string assembly);
    public abstract string[] GetExpectedBootLines (string assembly);
    public abstract string[] GetExpectedTypeLines (string assembly);

    protected string BuildFunctionAssignmentLine (string assembly, string method)
    {
        return $"global.DotNetJS_functions_{assembly}_{method} = exports.{assembly}.{method} || " +
               $"function() {{ throw new Error(\"Function 'dotnet.{assembly}.{method}' is not implemented.\"); }}();";
    }
}
