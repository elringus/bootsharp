using System.Reflection;
using Bootsharp;
using Bootsharp.Test;
using Microsoft.Extensions.DependencyInjection;

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

namespace Bootsharp.Test;

public interface IBackend;
public interface IFrontend;
public class Backend : IBackend;
public class Frontend : IFrontend;

public class JSTypesTest
{
    private readonly CustomAttributeData export = GetMockExportAttribute();
    private readonly CustomAttributeData import = GetMockImportAttribute();

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
        Assert.Equal(new object[] { typeof(IBackend) },
            (export.ConstructorArguments[0].Value as IReadOnlyCollection<CustomAttributeTypedArgument>).Select(a => a.Value));
        Assert.Equal("ExportNamePattern", GetNamedValue(export.NamedArguments, nameof(JSTypeAttribute.NamePattern)));
        Assert.Equal("ExportNameReplacement", GetNamedValue(export.NamedArguments, nameof(JSTypeAttribute.NameReplacement)));
        Assert.Equal("ExportInvokePattern", GetNamedValue(export.NamedArguments, nameof(JSTypeAttribute.InvokePattern)));
        Assert.Equal("ExportInvokeReplacement", GetNamedValue(export.NamedArguments, nameof(JSTypeAttribute.InvokeReplacement)));
    }

    [Fact]
    public void ImportParametersEqualArguments ()
    {
        Assert.Equal(new object[] { typeof(IFrontend) },
            (import.ConstructorArguments[0].Value as IReadOnlyCollection<CustomAttributeTypedArgument>).Select(a => a.Value));
        Assert.Equal("ImportNamePattern", GetNamedValue(import.NamedArguments, nameof(JSTypeAttribute.NamePattern)));
        Assert.Equal("ImportNameReplacement", GetNamedValue(import.NamedArguments, nameof(JSTypeAttribute.NameReplacement)));
        Assert.Equal("ImportInvokePattern", GetNamedValue(import.NamedArguments, nameof(JSTypeAttribute.InvokePattern)));
        Assert.Equal("ImportInvokeReplacement", GetNamedValue(import.NamedArguments, nameof(JSTypeAttribute.InvokeReplacement)));
        Assert.Equal("ImportEventPattern", GetNamedValue(import.NamedArguments, nameof(JSImportAttribute.EventPattern)));
        Assert.Equal("ImportEventReplacement", GetNamedValue(import.NamedArguments, nameof(JSImportAttribute.EventReplacement)));
    }

    [Fact]
    public void CanInjectGeneratedTypes ()
    {
        var provider = new ServiceCollection()
            .AddBootsharp()
            .BuildServiceProvider();
        Assert.IsType<global::Frontend.JSFrontend>(provider.GetRequiredService<IFrontend>());
    }

    [Fact]
    public void CanBuildGeneratedTypes ()
    {
        new ServiceCollection()
            .AddSingleton<IBackend, Backend>()
            .AddBootsharp()
            .BuildServiceProvider()
            .BuildBootsharp();
        Assert.IsType<Backend>(typeof(global::Backend.JSBackend)
            .GetField("handler", BindingFlags.NonPublic | BindingFlags.Static).GetValue(null));
    }

    [Fact]
    public void WhenMissingRequiredDependencyErrorIsThrown ()
    {
        Assert.Contains("Failed to build Bootsharp services: 'Bootsharp.Test.IBackend' dependency is not registered.",
            Assert.Throws<Error>(() => new ServiceCollection().AddBootsharp().BuildServiceProvider().BuildBootsharp()).Message);
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
