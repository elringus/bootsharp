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
        AddAssembly(With("[JSInvokable] public static void Foo () { }"));
        Execute();
        Assert.Empty(TestedContent);
    }

    [Fact]
    public void ImportsWithoutSpace ()
    {
        AddAssembly("asm", With(
            """
            public class Foo
            {
                [JSFunction] public static partial void Bar ();
                [JSEvent] public static partial void Baz ();
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
        AddAssembly("asm", With(
            """
            namespace Foo.Bar
            {
                public class Baz
                {
                    [JSFunction] public static partial void Nya ();
                    [JSEvent] public static partial void Far ();
                }
            }
            namespace Foo
            {
                public class Baz
                {
                    [JSFunction] public static partial void Nya ();
                    [JSEvent] public static partial void Far ();
                }
            }
            public class Baz
            {
                [JSFunction] public static partial void Nya ();
                [JSEvent] public static partial void Far ();
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
        AddAssembly("asm", With(
            """
            namespace Space;

            public record Info(string Foo);

            public class Foo
            {
                [JSFunction] public static partial Info? Bar (string a, int[] b);
                [JSFunction] public static partial Task Baz ();
                [JSFunction] public static partial Task<Info?> Nya (Info a);
                [JSEvent] public static partial void OnBar (Info? a, bool b);
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

                [JSImport("Space.bar", "Bootsharp")] internal static partial void Bar ();
                [JSImport("Space.baz", "Bootsharp")] internal static partial void Baz ();
                [JSImport("Space.nya", "Bootsharp")] internal static partial void Nya ();
                [JSImport("Space.onBar.broadcast", "Bootsharp")] internal static partial void OnBar ();
            }
            """);
    }
}
