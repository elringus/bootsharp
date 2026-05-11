namespace Bootsharp.Generate.Test;

public static class ImportPropertyTest
{
    public static TheoryData<string, string> Data { get; } = new() {
        // Can generate import property without namespace.
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
        // Can generate getter-only import property under namespace.
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
        // Can generate setter-only import property.
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
        // Ignores non-static properties.
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
