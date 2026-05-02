namespace Bootsharp.Common.Test;

public class ModulesTest
{
    [Fact]
    public void RegistersInterfaceExport ()
    {
        var export = new ExportModule(typeof(IBackend), null);
        Modules.Register(typeof(Backend), export);
        Assert.Equal(typeof(IBackend), Modules.Exports[typeof(Backend)].Handler);
    }

    [Fact]
    public void RegistersClassExport ()
    {
        var export = new ExportModule(typeof(Backend), null);
        Modules.Register(typeof(Backend), export);
        Assert.Equal(typeof(Backend), Modules.Exports[typeof(Backend)].Handler);
    }

    [Fact]
    public void RegistersImport ()
    {
        var import = new ImportModule(new Frontend());
        Modules.Register(typeof(IFrontend), import);
        Assert.IsType<Frontend>(Modules.Imports[typeof(IFrontend)].Instance);
    }
}
