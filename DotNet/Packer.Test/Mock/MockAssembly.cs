using System;
using System.Collections.Generic;

namespace Packer.Test;

public class MockAssembly
{
    public string Name { get; }
    public List<MockClass> Sources { get; } = new();

    public MockAssembly (string name = null)
    {
        Name = name ?? $"MockAssembly{Guid.NewGuid():N}.dll";
    }

    public static MockAssembly With (params string[] classLines)
    {
        return new MockAssembly().Add(classLines);
    }

    public static MockAssembly WithSpace (string @namespace, params string[] classLines)
    {
        return new MockAssembly().AddSpace(@namespace, classLines);
    }

    public MockAssembly Add (params string[] classLines)
    {
        Sources.Add(new MockClass { Lines = classLines });
        return this;
    }

    public MockAssembly AddSpace (string @namespace, params string[] classLines)
    {
        Sources.Add(new MockClass { Space = @namespace, Lines = classLines });
        return this;
    }
}
