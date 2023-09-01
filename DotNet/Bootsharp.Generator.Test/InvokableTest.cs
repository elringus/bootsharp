using System.Collections.Generic;

namespace Bootsharp.Generator.Test;

public static class InvokableTest
{
    public static IEnumerable<object[]> Data { get; } = new[] {
        // Generates dynamic dependencies registration.
        new object[] {
            """
            partial class Foo
            {
                [JSInvokable]
                public static void Bar ();
            }
            """,
            """
            partial class Foo
            {
                [ModuleInitializer]
                [DynamicDependency(DynamicallyAccessedMemberTypes.PublicMethods, "Foo", "GeneratorTest")]
                internal static void RegisterDynamicDependencies () { }
            }
            """
        },
        // Generates dynamic dependencies registration under namespace.
        new object[] {
            """
            namespace Space;

            partial class Foo
            {
                [JSInvokable]
                public static void Bar ();
            }
            """,
            """
            namespace Space;

            partial class Foo
            {
                [ModuleInitializer]
                [DynamicDependency(DynamicallyAccessedMemberTypes.PublicMethods, "Space.Foo", "GeneratorTest")]
                internal static void RegisterDynamicDependencies () { }
            }
            """
        },
        // Doesn't generate registration twice in case the class already has function.
        new object[] {
            """
            partial class Foo
            {
                [JSInvokable]
                public static void Bar ();
                [JSFunction]
                partial void Baz ();
            }
            """,
            """
            partial class Foo
            {
                [ModuleInitializer]
                [DynamicDependency(DynamicallyAccessedMemberTypes.PublicMethods, "Foo", "GeneratorTest")]
                internal static void RegisterDynamicDependencies () { }

                partial void Baz () => Function.InvokeVoid("Bindings.baz");
            }
            """
        },
        // Doesn't generate registration twice in case the class already has event.
        new object[] {
            """
            partial class Foo
            {
                [JSInvokable]
                public static void Bar ();
                [JSEvent]
                partial void OnBaz ();
            }
            """,
            """
            partial class Foo
            {
                [ModuleInitializer]
                [DynamicDependency(DynamicallyAccessedMemberTypes.PublicMethods, "Foo", "GeneratorTest")]
                internal static void RegisterDynamicDependencies () { }

                partial void OnBaz () => Event.Broadcast("Bindings.onBaz");
            }
            """
        }
    };
}
