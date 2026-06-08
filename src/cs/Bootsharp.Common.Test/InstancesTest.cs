using static Bootsharp.Instances;

namespace Bootsharp.Common.Test;

public class InstancesTest
{
    private interface IFoo;
    private interface IBar;
    private class Foo : IFoo;
    private class Bar : IBar;
    private class Proxy (int id) : JSProxy(id);
    private class SpecializedExport (object it) : Bootsharp.SpecializedExport(it);

    private class SpecializedImport (int id, object it) : Bootsharp.SpecializedImport(id)
    {
        protected internal override object Unwrap () => it;
    }

    private class DelegateProxy (int id) : JSProxy(id)
    {
        public void Invoke () { }
    }

    [Fact]
    public void CanExportAndDisposeInstance ()
    {
        var exported = new object();
        var id = Export(exported);
        Assert.Same(exported, Exported<object>(id));
        DisposeExported(id);
    }

    [Fact]
    public void IgnoresUnknownIdsOnDispose ()
    {
        var exported = new object();
        var id = Export(exported);
        DisposeExported(-1);
        Assert.Same(exported, Exported<object>(id));
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
        Assert.Equal(Export(new Proxy(1)), Export(new Proxy(1)));
    }

    [Fact]
    public void ShortCircuitsSpecializedExportsWrappingImportedProxy ()
    {
        RegisterExport(typeof(Bar), _ => new SpecializedExport(new Proxy(1)));
        Assert.Equal(Export(new Bar()), Export(new Bar()));
    }

    [Fact]
    public void GeneratesUniqueIdsForSpecializedExportsNotWrappingImportedProxy ()
    {
        RegisterExport(typeof(Foo), it => new SpecializedExport(it));
        Assert.NotEqual(Export(new Foo()), Export(new Foo()));
    }

    [Fact]
    public void ShortCircuitsRegisteredSpecializedExports ()
    {
        var exported = new Foo();
        RegisterExport(typeof(Foo), it => new SpecializedExport(it));
        Assert.Equal(Export(exported), Export(exported));
    }

    [Fact]
    public void ShortCircuitsImportedDelegates ()
    {
        Assert.Equal(Export(new DelegateProxy(1).Invoke), Export(new DelegateProxy(1).Invoke));
    }

    [Fact]
    public void ExportsZeroWhenInstanceIsNull ()
    {
        Assert.Equal(0, Export(default(object)));
    }

    [Fact]
    public void InvokesExportFactoryCallbacks ()
    {
        var exported = false;
        var disposed = false;
        RegisterExport(typeof(Bar), null, (_, _) => {
            exported = true;
            return () => disposed = true;
        });
        var id = Export(new Bar());
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
        DisposeImported(42);
    }

    [Fact]
    public void ShortCircuitsImportedExports ()
    {
        var exported = new object();
        Assert.Same(exported, Resolve<object>(Export(exported)));
    }

    [Fact]
    public void UnwrapsResolvedSpecializedImportsOfRefType ()
    {
        var imported = new Bar();
        RegisterImport(typeof(IBar), id => new SpecializedImport(id, imported));
        Assert.Same(imported, Resolve<IBar>(1));
        DisposeImported(1);
    }

    [Fact]
    public void UnwrapsResolvedSpecializedImportsOfValueType ()
    {
        var imported = DateTime.Now;
        RegisterImport(typeof(DateTime), id => new SpecializedImport(id, imported));
        Assert.Equal(imported, Resolve<DateTime>(1));
        DisposeImported(1);
    }

    [Fact]
    public void UnwrapsResolvedSpecializedExports ()
    {
        var exported = new Foo();
        RegisterExport(typeof(Foo), it => new SpecializedExport(it));
        Assert.Same(exported, Resolve<IFoo>(Export(exported)));
    }

    [Fact]
    public void ResolvesNullWhenIdIsZero ()
    {
        Assert.Null(Resolve<object>(0));
    }
}
