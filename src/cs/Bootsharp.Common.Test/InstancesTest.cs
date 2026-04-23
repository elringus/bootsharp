using static Bootsharp.Instances;

namespace Bootsharp.Common.Test;

public class InstancesTest
{
    [Fact]
    public void CaneExportAndDisposeInstance ()
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
    public void KeepsStableIdsForSameExports ()
    {
        var exported = new object();
        Assert.Equal(Export(exported), Export(exported));
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
        var imported = new object();
        Assert.Same(imported, Import(1, _ => imported));
        DisposeImported(1);
    }

    [Fact]
    public void CachesImportsUntilDisposed ()
    {
        var imported = new object();
        Import(42, _ => imported);
        Assert.Same(imported,
            // We don't use the factory here and ignore the fact that it returns another instance,
            // because we already have previous import associated with the '42' ID — it's the
            // responsibility of the JS side to let us know when the instance is disposed.
            Import(42, _ => new object()));
        // Here, we simulate JS side telling us to dispose the '42' instance.
        DisposeImported(42);
        // Now we exercise the factory and register the new instance as '42'.
        Assert.NotSame(imported, Import(42, _ => new object()));
    }
}
