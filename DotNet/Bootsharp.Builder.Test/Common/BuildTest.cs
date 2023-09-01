using System;
using System.IO;
using Microsoft.Build.Utilities.ProjectCreation;

namespace Bootsharp.Builder.Test;

public abstract class BuildTest : IDisposable
{
    protected MockProject Project { get; } = new();
    protected BuildBootsharp Task => Project.BuildTask;
    protected BuildEngine Engine => (BuildEngine)Task.BuildEngine;
    protected string GeneratedBindings => ReadGenerated("bindings.g.js");
    protected string GeneratedDeclarations => ReadGenerated("bindings.g.d.ts");
    protected string GeneratedResources => ReadGenerated("resources.g.js");

    public void Dispose ()
    {
        Project.Dispose();
    }

    protected void AddAssembly (string assemblyName, params MockSource[] sources)
    {
        Project.AddAssembly(new(assemblyName, sources));
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

    protected string ReadGenerated (string fileName)
    {
        var filePath = Path.Combine(Task.BuildDirectory, fileName);
        return File.Exists(filePath) ? File.ReadAllText(filePath) : null;
    }
}
