namespace Bootsharp.Publish.Test;

public class TypesTest
{
    [Fact]
    public void Temp ()
    {
        // TODO: Remove when coverlet bug is resolved: https://github.com/coverlet-coverage/coverlet/issues/1561
        _ = new InterfaceMeta { Kind = default, Instanced = false, Type = default, TypeSyntax = "", Name = "", Namespace = "", Methods = [] } with { Name = "foo" };
        _ = new InterfaceMethodMeta { Name = "", Meta = default } with { Name = "foo" };
        _ = new MethodMeta { Name = "", JSName = "", Arguments = default, Assembly = "", Kind = default, Space = "", JSSpace = "", ReturnValue = default } with { Assembly = "foo" };
        _ = new ArgumentMeta { Name = "", JSName = "", Value = default } with { Name = "foo" };
        _ = new ValueMeta { Type = default, Nullable = true, TypeSyntax = "", Void = true, Serialized = true, Async = true, JSTypeSyntax = "" } with { TypeSyntax = "foo" };
        _ = new Preferences { Event = [] } with { Function = [] };
    }
}
