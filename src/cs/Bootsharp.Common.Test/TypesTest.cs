using System.Reflection;
using Bootsharp;
using Bootsharp.Common.Test;

[assembly: JSExport(typeof(IBackend))]
[assembly: JSImport(typeof(IFrontend))]

namespace Bootsharp.Common.Test;

public class TypesTest
{
    private readonly CustomAttributeData export = GetMockExportAttribute();
    private readonly CustomAttributeData import = GetMockImportAttribute();

    [Fact]
    public void Records ()
    {
        // TODO: Remove when coverlet bug is resolved: https://github.com/coverlet-coverage/coverlet/issues/1561
        _ = new SolutionMeta { Assemblies = [], Interfaces = [], Methods = [], Crawled = [] } with { Assemblies = default };
        _ = new AssemblyMeta { Name = "", Bytes = [] } with { Name = "foo" };
        _ = new InterfaceMeta { Kind = default, TypeSyntax = "", Name = "", Namespace = "", Methods = [] } with { Name = "foo" };
        _ = new InterfaceMethodMeta { Name = "", Generated = default } with { Name = "foo" };
        _ = new MethodMeta { Name = "", JSName = "", Arguments = default, Assembly = "", Kind = default, Space = "", JSSpace = "", ReturnValue = default } with { Assembly = "foo" };
        _ = new ArgumentMeta { Name = "", JSName = "", Value = default } with { Name = "foo" };
        _ = new ValueMeta { Type = default, Nullable = true, TypeSyntax = "", Void = true, Serialized = true, Async = true, JSTypeSyntax = "" } with { TypeSyntax = "foo" };
        _ = new MockItem("") with { Id = "foo" };
        _ = new MockItemWithEnum(default) with { Enum = MockEnum.Bar };
        _ = new MockRecord(default) with { Items = new[] { new MockItem("") } };
    }

    [Fact]
    public void TypesAreAssigned ()
    {
        Assert.Equal([typeof(IBackend)], new JSExportAttribute(typeof(IBackend)).Types);
        Assert.Equal([typeof(IFrontend)], new JSImportAttribute(typeof(IFrontend)).Types);
    }

    [Fact]
    public void ExportParametersEqualArguments ()
    {
        Assert.Equal([typeof(IBackend)],
            (export.ConstructorArguments[0].Value as IReadOnlyCollection<CustomAttributeTypedArgument>).Select(a => a.Value));
    }

    [Fact]
    public void ImportParametersEqualArguments ()
    {
        Assert.Equal([typeof(IFrontend)],
            (import.ConstructorArguments[0].Value as IReadOnlyCollection<CustomAttributeTypedArgument>).Select(a => a.Value));
    }

    private static object GetNamedValue (IList<CustomAttributeNamedArgument> args, string key) =>
        args.First(a => a.MemberName == key).TypedValue.Value;
    private static CustomAttributeData GetMockExportAttribute () =>
        typeof(TypesTest).Assembly.CustomAttributes
            .First(a => a.AttributeType == typeof(JSExportAttribute));
    private static CustomAttributeData GetMockImportAttribute () =>
        typeof(TypesTest).Assembly.CustomAttributes
            .First(a => a.AttributeType == typeof(JSImportAttribute));
}
