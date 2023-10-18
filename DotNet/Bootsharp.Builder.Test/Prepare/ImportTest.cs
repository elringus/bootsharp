namespace Bootsharp.Builder.Test;

public class ImportTest : PrepareTest
{
    protected override string TestedContent => GeneratedImports;

    [Fact]
    public void WhenNothingInspectedNothingIsGenerated ()
    {
        Execute();
        Assert.Empty(TestedContent);
    }

    [Fact]
    public void WhenNoFunctionsOrEventsNothingIsGenerated ()
    {
        AddAssembly(With("[JSInvokable] public static void Foo () {}"));
        Execute();
        Assert.Empty(TestedContent);
    }

    [Fact]
    public void ImportsWithoutSpace ()
    {
        AddAssembly("asm.dll", With(
            """
            public class Foo
            {
                [JSFunction] public static void Bar () {}
                [JSEvent] public static void Baz () {}
            }
            """, false));
        Execute();
        Contains(
            """
            using System.Diagnostics.CodeAnalysis;
            using System.Runtime.CompilerServices;
            using System.Runtime.InteropServices.JavaScript;

            namespace Bootsharp.Imports;

            public partial class Foo
            {
                [ModuleInitializer]
                [DynamicDependency(DynamicallyAccessedMemberTypes.All, "Bootsharp.Imports.Foo", "asm")]
                internal static void RegisterDynamicDependencies ()
                {
                    Function.Set("Global.bar", Bar);
                    Function.Set("Global.baz", Baz);
                }
                [System.Runtime.InteropServices.JavaScript.JSImport("Global._bar", "Bootsharp")] internal static partial void Bar ();
                [System.Runtime.InteropServices.JavaScript.JSImport("Global._baz", "Bootsharp")] internal static partial void Baz ();
            }
            """);
    }

    [Fact]
    public void ImportsFromMultipleSpaces ()
    {
        AddAssembly("asm.dll", With(
            """
            namespace Foo.Bar
            {
                public class Baz
                {
                    [JSFunction] public static void Nya () {}
                    [JSEvent] public static void Far () {}
                }
            }
            namespace Foo
            {
                public class Baz
                {
                    [JSFunction] public static void Nya () {}
                    [JSEvent] public static void Far () {}
                }
            }
            public class Baz
            {
                [JSFunction] public static void Nya () {}
                [JSEvent] public static void Far () {}
            }
            """, false));
        Execute();
        Contains(
            """
            using System.Diagnostics.CodeAnalysis;
            using System.Runtime.CompilerServices;
            using System.Runtime.InteropServices.JavaScript;

            namespace Bootsharp.Imports;

            public partial class Baz
            {
                [ModuleInitializer]
                [DynamicDependency(DynamicallyAccessedMemberTypes.All, "Bootsharp.Imports.Baz", "asm")]
                internal static void RegisterDynamicDependencies ()
                {
                    Function.Set("Global.nya", Nya);
                    Function.Set("Global.far", Far);
                }
                [System.Runtime.InteropServices.JavaScript.JSImport("Global._nya", "Bootsharp")] internal static partial void Nya ();
                [System.Runtime.InteropServices.JavaScript.JSImport("Global._far", "Bootsharp")] internal static partial void Far ();
            }
            public partial class Foo_Baz
            {
                [ModuleInitializer]
                [DynamicDependency(DynamicallyAccessedMemberTypes.All, "Bootsharp.Imports.Foo_Baz", "asm")]
                internal static void RegisterDynamicDependencies ()
                {
                    Function.Set("Foo.nya", Nya);
                    Function.Set("Foo.far", Far);
                }
                [System.Runtime.InteropServices.JavaScript.JSImport("Foo._nya", "Bootsharp")] internal static partial void Nya ();
                [System.Runtime.InteropServices.JavaScript.JSImport("Foo._far", "Bootsharp")] internal static partial void Far ();
            }
            public partial class Foo_Bar_Baz
            {
                [ModuleInitializer]
                [DynamicDependency(DynamicallyAccessedMemberTypes.All, "Bootsharp.Imports.Foo_Bar_Baz", "asm")]
                internal static void RegisterDynamicDependencies ()
                {
                    Function.Set("Foo.Bar.nya", Nya);
                    Function.Set("Foo.Bar.far", Far);
                }
                [System.Runtime.InteropServices.JavaScript.JSImport("Foo.Bar._nya", "Bootsharp")] internal static partial void Nya ();
                [System.Runtime.InteropServices.JavaScript.JSImport("Foo.Bar._far", "Bootsharp")] internal static partial void Far ();
            }
            """);
    }

    [Fact]
    public void ImportsFunctionsAndEvents ()
    {
        AddAssembly("asm.dll", With(
            """
            namespace Space;

            public record Info(string Foo);

            public class Foo
            {
                [JSFunction] public static Info Bar (string a, int[] b) => default;
                [JSFunction] public static Task Baz () => default;
                [JSFunction] public static Task<Info?> Nya (Info a) => default;
                [JSEvent] public static void OnBar (Info? a, bool? b) {}
            }
            """, false));
        Execute();
        Contains(
            """
            using System.Diagnostics.CodeAnalysis;
            using System.Runtime.CompilerServices;
            using System.Runtime.InteropServices.JavaScript;

            namespace Bootsharp.Imports;

            public partial class Space_Foo
            {
                [ModuleInitializer]
                [DynamicDependency(DynamicallyAccessedMemberTypes.All, "Bootsharp.Imports.Space_Foo", "asm")]
                internal static void RegisterDynamicDependencies ()
                {
                    Function.Set("Space.bar", Bar);
                    Function.Set("Space.baz", Baz);
                    Function.Set("Space.nya", Nya);
                    Function.Set("Space.onBar", OnBar);
                }
                [System.Runtime.InteropServices.JavaScript.JSImport("Space._bar", "Bootsharp")] internal static partial global::System.String Bar (global::System.String a, global::System.Int32[] b);
                [System.Runtime.InteropServices.JavaScript.JSImport("Space._baz", "Bootsharp")] internal static partial global::System.Threading.Tasks.Task Baz ();
                [System.Runtime.InteropServices.JavaScript.JSImport("Space._nya", "Bootsharp")] internal static partial global::System.Threading.Tasks.Task<global::System.String?> Nya (global::System.String a);
                [System.Runtime.InteropServices.JavaScript.JSImport("Space._onBar", "Bootsharp")] internal static partial void OnBar (global::System.String? a, global::System.Boolean? b);
            }
            """);
    }

    [Fact]
    public void DoesntSerializeTypesThatShouldNotBeSerialized ()
    {
        AddAssembly("asm.dll", With(
            """
            namespace Space;

            public class Foo
            {
                [JSFunction] public static Task<Exception> Bar (bool a1, byte a2, char a3, short a4, long a5, int a6, float a7, double a8, nint a9, DateTime a10, DateTimeOffset a11, string a12, byte[] a13, int[] a14, double[] a15, string[] a16) => default;
                [JSFunction] public static Task<DateTime> Baz (bool? a1, byte? a2, char? a3, short? a4, long? a5, int? a6, float? a7, double? a8, nint? a9, DateTime? a10, DateTimeOffset? a11, string? a12, byte?[] a13, int?[] a14, double?[] a15, string?[] a16) => default;
            }
            """, false));
        Execute();
        Contains(
            """
            using System.Diagnostics.CodeAnalysis;
            using System.Runtime.CompilerServices;
            using System.Runtime.InteropServices.JavaScript;

            namespace Bootsharp.Imports;

            public partial class Space_Foo
            {
                [ModuleInitializer]
                [DynamicDependency(DynamicallyAccessedMemberTypes.All, "Bootsharp.Imports.Space_Foo", "asm")]
                internal static void RegisterDynamicDependencies ()
                {
                    Function.Set("Space.bar", Bar);
                    Function.Set("Space.baz", Baz);
                }
                [System.Runtime.InteropServices.JavaScript.JSImport("Space._bar", "Bootsharp")] internal static partial global::System.Threading.Tasks.Task<global::System.Exception> Bar (global::System.Boolean a1, global::System.Byte a2, global::System.Char a3, global::System.Int16 a4, [JSMarshalAs<JSType.BigInt>] global::System.Int64 a5, global::System.Int32 a6, global::System.Single a7, global::System.Double a8, global::System.IntPtr a9, [JSMarshalAs<JSType.Date>] global::System.DateTime a10, [JSMarshalAs<JSType.Date>] global::System.DateTimeOffset a11, global::System.String a12, global::System.Byte[] a13, global::System.Int32[] a14, global::System.Double[] a15, global::System.String[] a16);
                [System.Runtime.InteropServices.JavaScript.JSImport("Space._baz", "Bootsharp")] [return: JSMarshalAs<JSType.Promise<JSType.Date>>] internal static partial global::System.Threading.Tasks.Task<global::System.DateTime> Baz (global::System.Boolean? a1, global::System.Byte? a2, global::System.Char? a3, global::System.Int16? a4, [JSMarshalAs<JSType.BigInt>] global::System.Int64? a5, global::System.Int32? a6, global::System.Single? a7, global::System.Double? a8, global::System.IntPtr? a9, [JSMarshalAs<JSType.Date>] global::System.DateTime? a10, [JSMarshalAs<JSType.Date>] global::System.DateTimeOffset? a11, global::System.String? a12, global::System.Byte?[] a13, global::System.Int32?[] a14, global::System.Double?[] a15, global::System.String?[] a16);
            }
            """);
    }
}
