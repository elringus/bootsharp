using System;
using Xunit;

namespace Packer.Test;

public sealed class PublishDotNetJSTest : IDisposable
{
    private readonly MockData data = new();
    private readonly PublishDotNetJS task;

    public PublishDotNetJSTest ()
    {
        task = data.CreateTask();
        data.CreateBuildResources();
    }

    public void Dispose () => data.Dispose();

    [Fact]
    public void Test1 ()
    {
        task.Execute();
    }
}
