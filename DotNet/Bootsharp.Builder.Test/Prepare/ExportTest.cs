namespace Bootsharp.Builder.Test;

public class ExportTest : PrepareTest
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
    public void ExportsInvokableWithoutSpace ()
    {
        AddAssembly(With(
            """
            public class Foo
            {
                [JSInvokable] public static void Bar () {}
            }
            """, false));
        Execute();
        Contains(
            """
            using System.Runtime.InteropServices.JavaScript;
            using static Bootsharp.Serializer;

            namespace Bootsharp.Exports;

            public partial class Foo
            {
                [System.Runtime.InteropServices.JavaScript.JSExport] internal static void Bar () => global::Foo.Bar();
            }
            """);
    }

    [Fact]
    public void ExportsInvokableFromMultipleSpaces ()
    {
        AddAssembly(With(
            """
            namespace Foo.Bar
            {
                public class Baz
                {
                    [JSInvokable] public static void Nya () {}
                }
            }
            namespace Foo
            {
                public class Baz
                {
                    [JSInvokable] public static void Nya () {}
                }
            }
            public class Baz
            {
                [JSInvokable] public static void Nya () {}
            }
            """, false));
        Execute();
        Contains(
            """
            using System.Runtime.InteropServices.JavaScript;
            using static Bootsharp.Serializer;

            namespace Bootsharp.Exports;

            public partial class Baz
            {
                [System.Runtime.InteropServices.JavaScript.JSExport] internal static void Nya () => global::Baz.Nya();
            }
            public partial class Foo_Baz
            {
                [System.Runtime.InteropServices.JavaScript.JSExport] internal static void Nya () => global::Foo.Baz.Nya();
            }
            public partial class Foo_Bar_Baz
            {
                [System.Runtime.InteropServices.JavaScript.JSExport] internal static void Nya () => global::Foo.Bar.Baz.Nya();
            }
            """);
    }

    [Fact]
    public void ExportsMultipleInvokable ()
    {
        AddAssembly(With(
            """
            namespace Space;

            public record Info(string Foo);

            public class Foo
            {
                [JSInvokable] public static void Void () {}
                [JSInvokable] public static Info WithArgs (Info a, int[] b) => default;
                [JSInvokable] public static Task Async () => default;
                [JSInvokable] public static Task<Info?> AsyncWithArgs (Info? i) => default;
            }
            """, false));
        Execute();
        Contains(
            """
            using System.Runtime.InteropServices.JavaScript;
            using static Bootsharp.Serializer;

            namespace Bootsharp.Exports;

            public partial class Space_Foo
            {
                [System.Runtime.InteropServices.JavaScript.JSExport] internal static void Void () => global::Space.Foo.Void();
                [System.Runtime.InteropServices.JavaScript.JSExport] internal static global::System.String WithArgs (global::System.String a, global::System.Int32[] b) => Serialize(global::Space.Foo.WithArgs(Deserialize<global::Space.Info>(a), b));
                [System.Runtime.InteropServices.JavaScript.JSExport] internal static global::System.Threading.Tasks.Task Async () => global::Space.Foo.Async();
                [System.Runtime.InteropServices.JavaScript.JSExport] internal static async global::System.Threading.Tasks.Task<global::System.String?> AsyncWithArgs (global::System.String? i) => Serialize(await global::Space.Foo.AsyncWithArgs(Deserialize<global::Space.Info?>(i)));
            }
            """);
    }

    [Fact]
    public void DoesntSerializeTypesThatShouldNotBeSerialized ()
    {
        AddAssembly(With(
            """
            public class Foo
            {
                [JSInvokable] public static Task<Exception> Bar (bool a1, byte a2, char a3, short a4, long a5, int a6, float a7, double a8, nint a9, DateTime a10, DateTimeOffset a11, string a12, byte[] a13, int[] a14, double[] a15, string[] a16) => default;
                [JSInvokable] public static Task<DateTime> Baz (bool? a1, byte? a2, char? a3, short? a4, long? a5, int? a6, float? a7, double? a8, nint? a9, DateTime? a10, DateTimeOffset? a11, string? a12, byte?[] a13, int?[] a14, double?[] a15, string?[] a16) => default;
            }
            """, false));
        Execute();
        Contains(
            """
            using System.Runtime.InteropServices.JavaScript;
            using static Bootsharp.Serializer;

            namespace Bootsharp.Exports;

            public partial class Foo
            {
                [System.Runtime.InteropServices.JavaScript.JSExport] internal static global::System.Threading.Tasks.Task<global::System.Exception> Bar (global::System.Boolean a1, global::System.Byte a2, global::System.Char a3, global::System.Int16 a4, [JSMarshalAs<JSType.BigInt>] global::System.Int64 a5, global::System.Int32 a6, global::System.Single a7, global::System.Double a8, global::System.IntPtr a9, [JSMarshalAs<JSType.Date>] global::System.DateTime a10, [JSMarshalAs<JSType.Date>] global::System.DateTimeOffset a11, global::System.String a12, global::System.Byte[] a13, global::System.Int32[] a14, global::System.Double[] a15, global::System.String[] a16) => global::Foo.Bar(a1, a2, a3, a4, a5, a6, a7, a8, a9, a10, a11, a12, a13, a14, a15, a16);
                [System.Runtime.InteropServices.JavaScript.JSExport] [return: JSMarshalAs<JSType.Promise<JSType.Date>>] internal static global::System.Threading.Tasks.Task<global::System.DateTime> Baz (global::System.Boolean? a1, global::System.Byte? a2, global::System.Char? a3, global::System.Int16? a4, [JSMarshalAs<JSType.BigInt>] global::System.Int64? a5, global::System.Int32? a6, global::System.Single? a7, global::System.Double? a8, global::System.IntPtr? a9, [JSMarshalAs<JSType.Date>] global::System.DateTime? a10, [JSMarshalAs<JSType.Date>] global::System.DateTimeOffset? a11, global::System.String? a12, global::System.Byte?[] a13, global::System.Int32?[] a14, global::System.Double?[] a15, global::System.String?[] a16) => global::Foo.Baz(a1, a2, a3, a4, a5, a6, a7, a8, a9, a10, a11, a12, a13, a14, a15, a16);
            }
            """);
    }

    [Fact]
    public void SerializeTypesThatShouldBeSerialized ()
    {
        AddAssembly(With(
            """
            namespace Space;

            public record Info;

            public class Foo
            {
                [JSInvokable] public static Task<Info[]> A () => default;
                [JSInvokable] public static Task<byte[]> B () => default;
            }
            """, false));
        Execute();
        Contains(
            """
            using System.Runtime.InteropServices.JavaScript;
            using static Bootsharp.Serializer;

            namespace Bootsharp.Exports;

            public partial class Space_Foo
            {
                [System.Runtime.InteropServices.JavaScript.JSExport] internal static async global::System.Threading.Tasks.Task<global::System.String> A () => Serialize(await global::Space.Foo.A());
                [System.Runtime.InteropServices.JavaScript.JSExport] internal static async global::System.Threading.Tasks.Task<global::System.String> B () => Serialize(await global::Space.Foo.B());
            }
            """);
    }
}
