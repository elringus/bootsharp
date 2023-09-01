using System.Collections.Generic;

namespace Bootsharp.Generator.Test;

public static class EventTest
{
    public static IEnumerable<object[]> Data { get; } = new[] {
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
                [DynamicDependency(DynamicallyAccessedMemberTypes.PublicMethods, "Program", "JSInteropTest")]
                [DynamicDependency(DynamicallyAccessedMemberTypes.PublicMethods, "OtherAssembly.TestReflection", "OtherAssembly")]
                internal static void RegisterDynamicDependencies () { }

                partial void OnBar () => Event.Broadcast("Bindings/onBar");
            }
            """
        },
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
                public static partial void OnBar (string a, int b) => Event.Broadcast("Space/onBar", a, b);
            }
            """
        }
    };
}
