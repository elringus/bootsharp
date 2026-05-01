namespace Bootsharp.Generate.Test;

public static class ImportEventTest
{
    public static TheoryData<string, string> Data { get; } = new() {
        // Can generate import event without namespace and arguments.
        {
            """
            partial class Foo
            {
                [Import] static event Action? OnBar;
            }
            """,
            """
            unsafe partial class Foo
            {
                internal static void Bootsharp_Invoke_OnBar () => OnBar?.Invoke();
            }
            """
        },
        // Can generate import event with namespace and arguments.
        {
            """
            namespace Space;

            public static partial class Foo
            {
                [Import] public static event Action<string, int>? OnBar;
            }
            """,
            """
            namespace Space;

            public static unsafe partial class Foo
            {
                internal static void Bootsharp_Invoke_OnBar (global::System.String arg1, global::System.Int32 arg2) => OnBar?.Invoke(arg1, arg2);
            }
            """
        },
        // Ignores non-static events.
        {
            """
            partial class Foo
            {
                [Import] static event Action? OnBar;
                [Import] event Action? OnBaz;
            }
            """,
            """
            unsafe partial class Foo
            {
                internal static void Bootsharp_Invoke_OnBar () => OnBar?.Invoke();
            }
            """
        }
    };
}
