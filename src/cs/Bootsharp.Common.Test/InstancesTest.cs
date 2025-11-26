using static Bootsharp.Instances;

namespace Bootsharp.Common.Test;

public class InstancesTest
{
    [Fact]
    public void ThrowsWhenGettingUnregisteredInstance ()
    {
        Assert.Throws<Error>(() => Get(0));
    }

    [Fact]
    public void ThrowsWhenDisposingUnregisteredInstance ()
    {
        Assert.Throws<Error>(() => Dispose(0));
    }

    [Fact]
    public void CanRegisterGetAndDisposeInstance ()
    {
        var instance = new object();
        var id = Register(instance);
        Assert.Same(instance, Get(id));
        Dispose(id);
        Assert.Throws<Error>(() => Get(id));
    }

    [Fact]
    public void GeneratesUniqueIdsOnEachRegister ()
    {
        Assert.NotEqual(Register(new object()), Register(new object()));
    }

    [Fact]
    public void ReusesIdOfDisposedInstance ()
    {
        var id = Register(new object());
        Dispose(id);
        Assert.Equal(id, Register(new object()));
    }
}
