using System.Reflection;
using Bootsharp;
using Bootsharp.Common.Test;

[assembly: JSExport(
    typeof(IBackend),
    NamePattern = "ExportNamePattern",
    NameReplacement = "ExportNameReplacement",
    InvokePattern = "ExportInvokePattern",
    InvokeReplacement = "ExportInvokeReplacement"
)]

[assembly: JSImport(
    typeof(IFrontend),
    NamePattern = "ImportNamePattern",
    NameReplacement = "ImportNameReplacement",
    InvokePattern = "ImportInvokePattern",
    InvokeReplacement = "ImportInvokeReplacement",
    EventPattern = "ImportEventPattern",
    EventReplacement = "ImportEventReplacement"
)]

namespace Bootsharp.Common.Test;

public class TypesTest
{
    private readonly CustomAttributeData export = GetMockExportAttribute();
    private readonly CustomAttributeData import = GetMockImportAttribute();

    [Fact]
    public void Records ()
    {
        // TODO: Remove when coverlet bug is resolved: https://github.com/coverlet-coverage/coverlet/issues/1561
        _ = new SolutionMeta { Assemblies = [], Methods = [], Types = [] } with { Assemblies = default };
        _ = new AssemblyMeta { Name = "", Bytes = [] } with { Name = "foo" };
        _ = new MethodMeta { Name = "", JSName = "", Arguments = default, Assembly = "", Type = default, Space = "", JSSpace = "", ReturnType = default } with { Assembly = "foo" };
        _ = new ArgumentMeta { Name = "", JSName = "", Type = default } with { Name = "foo" };
        _ = new TypeMeta { Type = default, Nullable = true, Syntax = "", Void = true, ShouldSerialize = true, TaskLike = true, JSSyntax = "" } with { Syntax = "foo" };
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
    public void NameAndInvokeParametersAreNullByDefault ()
    {
        var attribute = new JSExportAttribute(typeof(IBackend));
        Assert.Null(attribute.NamePattern);
        Assert.Null(attribute.NameReplacement);
        Assert.Null(attribute.InvokePattern);
        Assert.Null(attribute.InvokeReplacement);
    }

    [Fact]
    public void EventParametersAreNullByDefault () // (defaults are in generator)
    {
        var attribute = new JSImportAttribute(typeof(IBackend));
        Assert.Null(attribute.EventPattern);
        Assert.Null(attribute.EventReplacement);
    }

    [Fact]
    public void ExportParametersEqualArguments ()
    {
        Assert.Equal([typeof(IBackend)],
            (export.ConstructorArguments[0].Value as IReadOnlyCollection<CustomAttributeTypedArgument>).Select(a => a.Value));
        Assert.Equal("ExportNamePattern", GetNamedValue(export.NamedArguments, nameof(JSTypeAttribute.NamePattern)));
        Assert.Equal("ExportNameReplacement", GetNamedValue(export.NamedArguments, nameof(JSTypeAttribute.NameReplacement)));
        Assert.Equal("ExportInvokePattern", GetNamedValue(export.NamedArguments, nameof(JSTypeAttribute.InvokePattern)));
        Assert.Equal("ExportInvokeReplacement", GetNamedValue(export.NamedArguments, nameof(JSTypeAttribute.InvokeReplacement)));
    }

    [Fact]
    public void ImportParametersEqualArguments ()
    {
        Assert.Equal([typeof(IFrontend)],
            (import.ConstructorArguments[0].Value as IReadOnlyCollection<CustomAttributeTypedArgument>).Select(a => a.Value));
        Assert.Equal("ImportNamePattern", GetNamedValue(import.NamedArguments, nameof(JSTypeAttribute.NamePattern)));
        Assert.Equal("ImportNameReplacement", GetNamedValue(import.NamedArguments, nameof(JSTypeAttribute.NameReplacement)));
        Assert.Equal("ImportInvokePattern", GetNamedValue(import.NamedArguments, nameof(JSTypeAttribute.InvokePattern)));
        Assert.Equal("ImportInvokeReplacement", GetNamedValue(import.NamedArguments, nameof(JSTypeAttribute.InvokeReplacement)));
        Assert.Equal("ImportEventPattern", GetNamedValue(import.NamedArguments, nameof(JSImportAttribute.EventPattern)));
        Assert.Equal("ImportEventReplacement", GetNamedValue(import.NamedArguments, nameof(JSImportAttribute.EventReplacement)));
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
