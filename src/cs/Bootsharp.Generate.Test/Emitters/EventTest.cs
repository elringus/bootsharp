namespace Bootsharp.Generate.Test;

public static class EventTest
{
    public static IEnumerable<object[]> Data { get; } = new[] {
        // Can generate event binding without namespace and arguments.
        [
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
                [DynamicDependency(DynamicallyAccessedMemberTypes.All, "Foo", "GeneratorTest")]
                internal static void RegisterDynamicDependencies () { }

                partial void OnBar () => Get<global::System.Action>("Global.onBar")();
            }
            """
        ],
        // Can generate event binding with namespace and arguments.
        [
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
                [DynamicDependency(DynamicallyAccessedMemberTypes.All, "Space.Foo", "GeneratorTest")]
                internal static void RegisterDynamicDependencies () { }

                public static partial void OnBar (global::System.String a, global::System.Int32 b) => Get<global::System.Action<global::System.String, global::System.Int32>>("Space.onBar")(a, b);
            }
            """
        ],
        // Can generate event binding with serialized parameters.
        new object[] {
            """
            public record Info(string Baz);

            public static partial class Foo
            {
                [JSEvent]
                public static partial void OnInfo (Info info);
            }
            """,
            """
            public static partial class Foo
            {
                [ModuleInitializer]
                [DynamicDependency(DynamicallyAccessedMemberTypes.All, "Foo", "GeneratorTest")]
                internal static void RegisterDynamicDependencies () { }

                public static partial void OnInfo (global::Info info) => Get<global::System.Action<global::System.String>>("Global.onInfo")(Serialize(info));
            }
            """
        }
    };
}
