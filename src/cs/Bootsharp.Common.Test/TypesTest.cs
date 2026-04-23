using System.Reflection;
using Bootsharp;

[assembly: Export(typeof(IBackend))]
[assembly: Import(typeof(IFrontend))]

namespace Bootsharp.Common.Test;

public class TypesTest
{
    private readonly CustomAttributeData export = GetMockExportAttribute();
    private readonly CustomAttributeData import = GetMockImportAttribute();

    [Fact]
    public void TypesAreAssigned ()
    {
        Assert.Equal([typeof(IBackend)], new ExportAttribute(typeof(IBackend)).Types);
        Assert.Equal([typeof(IFrontend)], new ImportAttribute(typeof(IFrontend)).Types);
        Assert.Equal("Space", (new PreferencesAttribute { Space = ["Space"] }).Space[0]);
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

    private static CustomAttributeData GetMockExportAttribute () =>
        typeof(TypesTest).Assembly.CustomAttributes
            .First(a => a.AttributeType == typeof(ExportAttribute));
    private static CustomAttributeData GetMockImportAttribute () =>
        typeof(TypesTest).Assembly.CustomAttributes
            .First(a => a.AttributeType == typeof(ImportAttribute));
}
