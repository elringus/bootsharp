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
        Assert.Equal(typeof(IBackend), BindingRegistry.Exports[typeof(global::Backend.JSBackend)].Api);
        Assert.IsType<Func<object, object>>(BindingRegistry.Exports[typeof(global::Backend.JSBackend)].Factory);
    }

    [Fact]
    public void RegistersImports ()
    {
        Assert.IsType<global::Frontend.JSFrontend>(BindingRegistry.Imports[typeof(IFrontend)].Implementation);
    }
}
