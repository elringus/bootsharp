using static Bootsharp.Instances;

namespace Bootsharp.Common.Test;

public class InstancesTest
{
    private interface IFoo;
    private interface IBar;
    private class Foo : IFoo;
    private class Bar : IBar;
    private class Proxy (int id) : JSProxy(id);

    [Fact]
    public void CanExportAndDisposeInstance ()
    {
        var exported = new object();
        var id = Export(exported);
        Assert.Same(exported, Exported<object>(id));
        DisposeExported(id);
    }

    [Fact]
    public void GeneratesUniqueIdsForUniqueExports ()
    {
        Assert.NotEqual(Export(new object()), Export(new object()));
    }

    [Fact]
    public void ShortCircuitsRegisteredExports ()
    {
        var exported = new object();
        Assert.Equal(Export(exported), Export(exported));
    }

    [Fact]
    public void ShortCircuitsImportedProxies ()
    {
        Assert.Equal(42, Export(new Proxy(42)));
    }

    [Fact]
    public void InvokesExportFactoryCallbacks ()
    {
        var exported = false;
        var disposed = false;
        var id = Export(new object(), (_, _) => {
            exported = true;
            return () => disposed = true;
        });
        Assert.True(exported);
        Assert.False(disposed);
        DisposeExported(id);
        Assert.True(disposed);
    }

    [Fact]
    public void CanImportAndDisposeInstance ()
    {
        var imported = new Foo();
        RegisterImport(typeof(IFoo), _ => imported);
        Assert.Same(imported, Resolve<IFoo>(1));
        DisposeImported(1);
    }

    [Fact]
    public void ShortCircuitsRegisteredImportsUntilDisposed ()
    {
        var imported = new Bar();
        RegisterImport(typeof(IBar), _ => imported);
        Resolve<IBar>(42);
        RegisterImport(typeof(IBar), _ => new Bar());
        Assert.Same(imported,
            // We already have previous import associated with the '42' ID — the registered
            // factory should not be invoked again; it's the responsibility of the JS side
            // to let us know when the instance is disposed.
            Resolve<IBar>(42));
        // Here, we simulate JS side telling us to dispose the '42' instance.
        DisposeImported(42);
        // Now we exercise the factory and register the new instance as '42'.
        Assert.NotSame(imported, Resolve<IBar>(42));
    }

    [Fact]
    public void ShortCircuitsImportedExports ()
    {
        var exported = new object();
        Assert.Same(exported, Resolve<object>(Export(exported)));
    }
}
