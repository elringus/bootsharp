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
            using System.Runtime.InteropServices.JavaScript;
            using System.Diagnostics.CodeAnalysis;
            using System.Runtime.CompilerServices;

            namespace Bootsharp;

            public partial class InteropImports_Foo
            {
                [ModuleInitializer]
                [DynamicDependency(DynamicallyAccessedMemberTypes.All, "Bootsharp.InteropImports_Foo", "asm")]
                internal static void RegisterDynamicDependencies ()
                {
                    Function.Set("Global.bar", Bar);
                    Function.Set("Global.baz.broadcast", Baz);
                }
                [JSImport("Global.bar", "Bootsharp")] internal static partial void Bar ();
                [JSImport("Global.baz.broadcast", "Bootsharp")] internal static partial void Baz ();
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
            using System.Runtime.InteropServices.JavaScript;
            using System.Diagnostics.CodeAnalysis;
            using System.Runtime.CompilerServices;

            namespace Bootsharp;

            public partial class InteropImports_Baz
            {
                [ModuleInitializer]
                [DynamicDependency(DynamicallyAccessedMemberTypes.All, "Bootsharp.InteropImports_Baz", "asm")]
                internal static void RegisterDynamicDependencies ()
                {
                    Function.Set("Global.nya", Nya);
                    Function.Set("Global.far.broadcast", Far);
                }
                [JSImport("Global.nya", "Bootsharp")] internal static partial void Nya ();
                [JSImport("Global.far.broadcast", "Bootsharp")] internal static partial void Far ();
            }
            public partial class InteropImports_Foo_Baz
            {
                [ModuleInitializer]
                [DynamicDependency(DynamicallyAccessedMemberTypes.All, "Bootsharp.InteropImports_Foo_Baz", "asm")]
                internal static void RegisterDynamicDependencies ()
                {
                    Function.Set("Foo.nya", Nya);
                    Function.Set("Foo.far.broadcast", Far);
                }
                [JSImport("Foo.nya", "Bootsharp")] internal static partial void Nya ();
                [JSImport("Foo.far.broadcast", "Bootsharp")] internal static partial void Far ();
            }
            public partial class InteropImports_Foo_Bar_Baz
            {
                [ModuleInitializer]
                [DynamicDependency(DynamicallyAccessedMemberTypes.All, "Bootsharp.InteropImports_Foo_Bar_Baz", "asm")]
                internal static void RegisterDynamicDependencies ()
                {
                    Function.Set("Foo.Bar.nya", Nya);
                    Function.Set("Foo.Bar.far.broadcast", Far);
                }
                [JSImport("Foo.Bar.nya", "Bootsharp")] internal static partial void Nya ();
                [JSImport("Foo.Bar.far.broadcast", "Bootsharp")] internal static partial void Far ();
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
                [JSFunction] public static Info? Bar (string a, int[] b) => default;
                [JSFunction] public static Task Baz () => default;
                [JSFunction] public static Task<Info?> Nya (Info a) => default;
                [JSEvent] public static void OnBar (Info? a, bool? b) {}
            }
            """, false));
        Execute();
        Contains(
            """
            using System.Runtime.InteropServices.JavaScript;
            using System.Diagnostics.CodeAnalysis;
            using System.Runtime.CompilerServices;

            namespace Bootsharp;

            public partial class InteropImports_Space_Foo
            {
                [ModuleInitializer]
                [DynamicDependency(DynamicallyAccessedMemberTypes.All, "Bootsharp.InteropImports_Space_Foo", "asm")]
                internal static void RegisterDynamicDependencies ()
                {
                    Function.Set("Space.bar", Bar);
                    Function.Set("Space.baz", Baz);
                    Function.Set("Space.nya", Nya);
                    Function.Set("Space.onBar.broadcast", OnBar);
                }
                [JSImport("Space.bar", "Bootsharp")] internal static partial global::System.String? Bar (global::System.String a, global::System.Int32[] b);
                [JSImport("Space.baz", "Bootsharp")] internal static partial global::System.Threading.Tasks.Task Baz ();
                [JSImport("Space.nya", "Bootsharp")] internal static partial global::System.Threading.Tasks.Task<global::System.String?> Nya (global::System.String a);
                [JSImport("Space.onBar.broadcast", "Bootsharp")] internal static partial void OnBar (global::System.String? a, global::System.Boolean? b);
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
                [JSFunction] public static Task<int[]> Bar (bool a1, byte a2, char a3, short a4, long a5, int a6, float a7, double a8, nint a9, DateTime a10, DateTimeOffset a11, string a12, byte[] a13, int[] a14, double[] a15, string[] a16) => default;
                [JSFunction] public static Task<int?[]> Baz (bool? a1, byte? a2, char? a3, short? a4, long? a5, int? a6, float? a7, double? a8, nint? a9, DateTime? a10, DateTimeOffset? a11, string? a12, byte?[] a13, int?[] a14, double?[] a15, string?[] a16) => default;
            }
            """, false));
        Execute();
        Contains(
            """
            using System.Runtime.InteropServices.JavaScript;
            using System.Diagnostics.CodeAnalysis;
            using System.Runtime.CompilerServices;

            namespace Bootsharp;

            public partial class InteropImports_Space_Foo
            {
                [ModuleInitializer]
                [DynamicDependency(DynamicallyAccessedMemberTypes.All, "Bootsharp.InteropImports_Space_Foo", "asm")]
                internal static void RegisterDynamicDependencies ()
                {
                    Function.Set("Space.bar", Bar);
                    Function.Set("Space.baz", Baz);
                }
                [JSImport("Space.bar", "Bootsharp")] internal static partial global::System.Threading.Tasks.Task<global::System.Int32[]> Bar (global::System.Boolean a1, global::System.Byte a2, global::System.Char a3, global::System.Int16 a4, global::System.Int64 a5, global::System.Int32 a6, global::System.Single a7, global::System.Double a8, global::System.IntPtr a9, global::System.DateTime a10, global::System.DateTimeOffset a11, global::System.String a12, global::System.Byte[] a13, global::System.Int32[] a14, global::System.Double[] a15, global::System.String[] a16);
                [JSImport("Space.baz", "Bootsharp")] internal static partial global::System.Threading.Tasks.Task<global::System.Int32?[]> Baz (global::System.Boolean? a1, global::System.Byte? a2, global::System.Char? a3, global::System.Int16? a4, global::System.Int64? a5, global::System.Int32? a6, global::System.Single? a7, global::System.Double? a8, global::System.IntPtr? a9, global::System.DateTime? a10, global::System.DateTimeOffset? a11, global::System.String? a12, global::System.Byte?[] a13, global::System.Int32?[] a14, global::System.Double?[] a15, global::System.String?[] a16);
            }
            """);
    }
}
