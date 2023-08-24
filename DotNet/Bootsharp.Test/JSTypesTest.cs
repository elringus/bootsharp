using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Bootsharp;
using Bootsharp.Test;
using Xunit;

[assembly: JSExport(
    typeof(JSTypesTest.IMockA),
    typeof(JSTypesTest.IMockB),
    NamePattern = "ExportNamePattern",
    NameReplacement = "ExportNameReplacement",
    InvokePattern = "ExportInvokePattern",
    InvokeReplacement = "ExportInvokeReplacement"
)]

[assembly: JSImport(
    typeof(JSTypesTest.IMockB),
    typeof(JSTypesTest.IMockA),
    NamePattern = "ImportNamePattern",
    NameReplacement = "ImportNameReplacement",
    InvokePattern = "ImportInvokePattern",
    InvokeReplacement = "ImportInvokeReplacement",
    EventPattern = "ImportEventPattern",
    EventReplacement = "ImportEventReplacement"
)]

namespace Bootsharp.Test;

public class JSTypesTest
{
    public interface IMockA;
    public interface IMockB;

    private readonly CustomAttributeData export = GetMockExportAttribute();
    private readonly CustomAttributeData import = GetMockImportAttribute();

    [Fact]
    public void NameAndInvokeParametersAreNullByDefault ()
    {
        var attribute = new JSExportAttribute(typeof(IMockA));
        Assert.Null(attribute.NamePattern);
        Assert.Null(attribute.NameReplacement);
        Assert.Null(attribute.InvokePattern);
        Assert.Null(attribute.InvokeReplacement);
    }

    [Fact]
    public void EventParametersAreNotNullByDefault ()
    {
        var attribute = new JSImportAttribute(typeof(IMockA));
        Assert.Equal(@"(^Notify)(\S+)", attribute.EventPattern);
        Assert.Equal("On$2", attribute.EventReplacement);
    }

    [Fact]
    public void ExportParametersEqualArguments ()
    {
        Assert.Equal(new object[] { typeof(IMockA), typeof(IMockB) },
            (export.ConstructorArguments[0].Value as IReadOnlyCollection<CustomAttributeTypedArgument>).Select(a => a.Value));
        Assert.Equal("ExportNamePattern", GetNamedValue(export.NamedArguments, nameof(JSTypeAttribute.NamePattern)));
        Assert.Equal("ExportNameReplacement", GetNamedValue(export.NamedArguments, nameof(JSTypeAttribute.NameReplacement)));
        Assert.Equal("ExportInvokePattern", GetNamedValue(export.NamedArguments, nameof(JSTypeAttribute.InvokePattern)));
        Assert.Equal("ExportInvokeReplacement", GetNamedValue(export.NamedArguments, nameof(JSTypeAttribute.InvokeReplacement)));
    }

    [Fact]
    public void ImportParametersEqualArguments ()
    {
        Assert.Equal(new object[] { typeof(IMockB), typeof(IMockA) },
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
        typeof(JSTypesTest).Assembly.CustomAttributes
            .First(a => a.AttributeType == typeof(JSExportAttribute));
    private static CustomAttributeData GetMockImportAttribute () =>
        typeof(JSTypesTest).Assembly.CustomAttributes
            .First(a => a.AttributeType == typeof(JSImportAttribute));
}
