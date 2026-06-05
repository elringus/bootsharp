using System.Reflection;
using Bootsharp;

[assembly: Export(typeof(IBackend))]
[assembly: Import(typeof(IFrontend))]

namespace Bootsharp.Common.Test;

public class TypesTest
{
    private class SpecializedImport (int id) : Bootsharp.SpecializedImport(id);
    private readonly CustomAttributeData export = GetMockExportAttribute();
    private readonly CustomAttributeData import = GetMockImportAttribute();

    [Fact]
    public void TypesAreAssigned ()
    {
        Assert.Equal([typeof(IBackend)], new ExportAttribute(typeof(IBackend)).Types);
        Assert.Equal([typeof(IFrontend)], new ImportAttribute(typeof(IFrontend)).Types);
        Assert.Equal(typeof(IBackend), new SpecializeExportAttribute(typeof(IBackend)).Clr);
        Assert.Equal(typeof(IFrontend), new SpecializeImportAttribute(typeof(IFrontend)).Clr);
        Assert.Equal("CS", new SpecializeImportAttribute(typeof(IFrontend), CS: "CS").CS);
        Assert.Equal("JS", new SpecializeImportAttribute(typeof(IFrontend), JS: "JS").JS);
        Assert.Equal("JSCtor", new SpecializeImportAttribute(typeof(IFrontend), JSCtor: "JSCtor").JSCtor);
        Assert.Equal("Decl", new SpecializeImportAttribute(typeof(IFrontend), Decl: "Decl").Decl);
    }

    [Fact]
    public void ExportParametersEqualArguments ()
    {
        Assert.Equal([typeof(IBackend)],
            (export.ConstructorArguments[0].Value as IReadOnlyCollection<CustomAttributeTypedArgument>)
            .Select(a => a.Value));
    }

    [Fact]
    public void ImportParametersEqualArguments ()
    {
        Assert.Equal([typeof(IFrontend)],
            (import.ConstructorArguments[0].Value as IReadOnlyCollection<CustomAttributeTypedArgument>)
            .Select(a => a.Value));
    }

    [Fact]
    public void SpecializedImportUnwrapsToItself ()
    {
        var imported = new SpecializedImport(1);
        Assert.Same(imported, imported.Unwrap());
    }

    private static CustomAttributeData GetMockExportAttribute () =>
        typeof(TypesTest).Assembly.CustomAttributes
            .First(a => a.AttributeType == typeof(ExportAttribute));
    private static CustomAttributeData GetMockImportAttribute () =>
        typeof(TypesTest).Assembly.CustomAttributes
            .First(a => a.AttributeType == typeof(ImportAttribute));
}
