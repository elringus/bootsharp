namespace Bootsharp.Builder.Test;

public class ExportTest : PrepareBootsharpTest
{
    protected override string TestedContent => GeneratedExports;

    [Fact]
    public void WhenNothingInspectedNothingIsGenerated ()
    {
        Execute();
        Assert.Empty(TestedContent);
    }

    [Fact]
    public void WhenNoInvokablesNothingIsGenerated ()
    {
        AddAssembly(With("[JSFunction] public static void Foo () {}"));
        Execute();
        Assert.Empty(TestedContent);
    }

    [Fact]
    public void ExportsInvokable ()
    {
        AddAssembly(With(
            """
            namespace Space;

            public record Info(string Foo);

            public class Foo
            {
                [JSInvokable] public static void Void () {}
                [JSInvokable] public static global::Space.Info? WithArgs (string a, global::System.Int32[] b) => default;
                [JSInvokable] public static global::System.Threading.Tasks.Task Async () => default;
                [JSInvokable] public static Task<Info?> AsyncWithArgs (Info i) => default;
            }
            """, false));
        Execute();
        Contains(
            """
            using System.Runtime.InteropServices.JavaScript;
            using static Bootsharp.Serializer;

            namespace Bootsharp;

            public partial class InteropExports_Space_Foo
            {
                [JSExport] internal static void Void () => global::Space.Foo.Void();
                [JSExport] internal static global::System.String? WithArgs (global::System.String a, global::System.Int32[] b) => Serialize(global::Space.Foo.WithArgs(a, b));
                [JSExport] internal static global::System.Threading.Tasks.Task Async () => global::Space.Foo.Async();
                [JSExport] internal static async global::System.Threading.Tasks.Task<global::System.String?> AsyncWithArgs (global::System.String i) => Serialize(await global::Space.Foo.AsyncWithArgs(Deserialize<global::Space.Info?>(i)));
            }
            """);
    }
}
