using System;
using Microsoft.Build.Utilities.ProjectCreation;

namespace Bootsharp.Packer.Test;

public abstract class BuildTest : IDisposable
{
    protected MockData Data { get; } = new();
    protected PublishBootsharp Task => Data.Task;
    protected BuildEngine Engine => (BuildEngine)Task.BuildEngine;

    public void Dispose ()
    {
        Data.Dispose();
        GC.SuppressFinalize(this);
    }

    protected void AddAssembly (string assemblyName, params MockSource[] sources)
    {
        Data.AddAssembly(new(assemblyName, sources));
    }

    protected void AddAssembly (params MockSource[] sources)
    {
        AddAssembly($"MockAssembly{Guid.NewGuid():N}.dll", sources);
    }

    protected MockSource With (string @namespace, string code, bool wrapInClass = true)
    {
        return new(@namespace, code, wrapInClass);
    }

    protected MockSource With (string code, bool wrapInClass = true)
    {
        return With(null, code, wrapInClass);
    }
}
