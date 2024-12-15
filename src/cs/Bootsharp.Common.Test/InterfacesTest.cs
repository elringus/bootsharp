namespace Bootsharp.Common.Test;

public class InterfacesTest
{
    [Fact]
    public void RegistersExports ()
    {
        var export = new ExportInterface(typeof(IBackend), null);
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
