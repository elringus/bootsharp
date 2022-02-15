using System;

namespace Packer.Test;

public class MockClass
{
    public string Space { get; init; } = "MockNamespace";
    public string Name { get; init; } = $"MockClass{Guid.NewGuid():N}";
    public string[] Lines { get; init; } = Array.Empty<string>();

    public MockClass (string space = null, string name = null)
    {
        if (space != null) Space = space;
        if (name != null) Name = name;
    }
}
