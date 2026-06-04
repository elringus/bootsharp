namespace Bootsharp.Generate.Test;

public static class ImportPropertyTest
{
    public static TheoryData<string, string> Data { get; } = new() {
        // Property with both accessors under the root namespace.
        {
            """
            partial class Foo
            {
                [Import] static partial int Counter { get; set; }
            }
            """,
            """
            unsafe partial class Foo
            {
                static partial global::System.Int32 Counter { get => Bootsharp_GetCounter(); set => Bootsharp_SetCounter(value); }
                public static delegate* managed<global::System.Int32> Bootsharp_GetCounter;
                public static delegate* managed<global::System.Int32, void> Bootsharp_SetCounter;
            }
            """
        },
        // Getter-only property under a namespace.
        {
            """
            namespace Space;

            public static partial class Foo
            {
                [Import] public static partial string Label { get; }
            }
            """,
            """
            namespace Space;

            public static unsafe partial class Foo
            {
                public static partial global::System.String Label { get => Bootsharp_GetLabel(); }
                public static delegate* managed<global::System.String> Bootsharp_GetLabel;
            }
            """
        },
        // Setter-only property.
        {
            """
            partial class Foo
            {
                [Import] static partial bool Active { set; }
            }
            """,
            """
            unsafe partial class Foo
            {
                static partial global::System.Boolean Active { set => Bootsharp_SetActive(value); }
                public static delegate* managed<global::System.Boolean, void> Bootsharp_SetActive;
            }
            """
        },
        // Non-static properties are ignored.
        {
            """
            partial class Foo
            {
                [Import] static partial int Counter { get; set; }
                [Import] int Other { get; set; }
            }
            """,
            """
            unsafe partial class Foo
            {
                static partial global::System.Int32 Counter { get => Bootsharp_GetCounter(); set => Bootsharp_SetCounter(value); }
                public static delegate* managed<global::System.Int32> Bootsharp_GetCounter;
                public static delegate* managed<global::System.Int32, void> Bootsharp_SetCounter;
            }
            """
        }
    };
}
