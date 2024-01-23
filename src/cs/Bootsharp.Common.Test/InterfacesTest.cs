namespace Bootsharp.Common.Test;

public class InterfacesTest
{
    [Fact]
    public void Records ()
    {
        // TODO: Remove once coverlet properly handles record coverage.
        _ = new ExportInterface(default, default) with { Interface = typeof(int) };
        _ = new ImportInterface(default) with { Instance = "" };
    }

    [Fact]
    public void RegistersExports ()
    {
        var export = new ExportInterface(typeof(IBackend), default);
        Interfaces.Register(typeof(Backend), export);
        Assert.Equal(typeof(IBackend), Interfaces.Exports[typeof(Backend)].Interface);
    }

    [Fact]
    public void RegistersImports ()
    {
        var import = new ImportInterface(new Frontend());
        Interfaces.Register(typeof(IFrontend), import);
        Assert.IsType<Frontend>(Interfaces.Imports[typeof(IFrontend)].Instance);
    }
}
