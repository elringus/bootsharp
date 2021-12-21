using System.Runtime.CompilerServices;

namespace Packer.Test;

public abstract class MockSource
{
    public string SourceFilePath { get; private set; }

    protected MockSource () => TraceFilePath();

    public abstract string[] GetExpectedInitLines ();
    public abstract string[] GetExpectedBootLines ();
    public abstract string[] GetExpectedTypeLines ();

    private void TraceFilePath ([CallerFilePath] string path = "")
    {
        SourceFilePath = path;
    }
}
