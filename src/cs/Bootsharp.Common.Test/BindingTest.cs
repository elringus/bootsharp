namespace Bootsharp.Common.Test;

public class BindingTest
{
    [Fact]
    public void Records ()
    {
        // TODO: Remove once coverlet properly handles record coverage.
        _ = new ExportBinding(default, default) with { Api = typeof(int) };
        _ = new ImportBinding(default) with { Implementation = "" };
    }

    [Fact]
    public void RegistersExports ()
    {
        var binding = new ExportBinding(typeof(IBackend), default);
        BindingRegistry.Register(typeof(Backend), binding);
        Assert.Equal(typeof(IBackend), BindingRegistry.Exports[typeof(Backend)].Api);
    }

    [Fact]
    public void RegistersImports ()
    {
        var binding = new ImportBinding(new Frontend());
        BindingRegistry.Register(typeof(IFrontend), binding);
        Assert.IsType<Frontend>(BindingRegistry.Imports[typeof(IFrontend)].Implementation);
    }
}
