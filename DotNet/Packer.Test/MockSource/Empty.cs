using System;

namespace Packer.Test;

public class Empty : MockSource
{
    public override string[] GetExpectedInitLines () => Array.Empty<string>();
    public override string[] GetExpectedBootLines () => Array.Empty<string>();
    public override string[] GetExpectedTypeLines () => Array.Empty<string>();
}
