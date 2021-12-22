using System;
using Microsoft.Build.Utilities.ProjectCreation;

namespace Packer.Test;

public abstract class BuildTest : IDisposable
{
    protected readonly MockData Data = new();
    protected PublishDotNetJS Task => Data.Task;
    protected BuildEngine Engine => (BuildEngine)Task.BuildEngine;

    public void Dispose ()
    {
        Data.Dispose();
        GC.SuppressFinalize(this);
    }
}
