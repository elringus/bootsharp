using System;

namespace Packer.Test;

public class Empty : MockSource
{
    public override string[] GetExpectedInitLines (string assembly) => Array.Empty<string>();
    public override string[] GetExpectedBootLines (string assembly) => Array.Empty<string>();
    public override string[] GetExpectedTypeLines (string assembly) => Array.Empty<string>();
}
