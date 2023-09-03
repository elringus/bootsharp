using System.Collections.Generic;

namespace Bootsharp.Generator.Test;

public static class EventTest
{
    public static IEnumerable<object[]> Data { get; } = new[] {
        // Can generate event binding without namespace and arguments.
        new object[] {
            """
            partial class Foo
            {
                [JSEvent]
                partial void OnBar ();
            }
            """,
            """
            partial class Foo
            {
                [ModuleInitializer]
                [DynamicDependency(DynamicallyAccessedMemberTypes.PublicMethods, "Foo", "GeneratorTest")]
                internal static void RegisterDynamicDependencies () { }

                partial void OnBar () => Event.Broadcast("Global.onBar");
            }
            """
        },
        // Can generate event binding with namespace and arguments.
        new object[] {
            """
            namespace Space;

            public static partial class Foo
            {
                [JSEvent]
                public static partial void OnBar (string a, int b);
            }
            """,
            """
            namespace Space;

            public static partial class Foo
            {
                [ModuleInitializer]
                [DynamicDependency(DynamicallyAccessedMemberTypes.PublicMethods, "Space.Foo", "GeneratorTest")]
                internal static void RegisterDynamicDependencies () { }

                public static partial void OnBar (string a, int b) => Event.Broadcast("Space.onBar", a, b);
            }
            """
        }
    };
}
