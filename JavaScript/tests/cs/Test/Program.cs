using Bootsharp;

namespace Test;

public static partial class Program
{
    private static bool mainInvoked;

    public static void Main () => mainInvoked = true;

    [JSInvokable]
    public static bool IsMainInvoked () => mainInvoked;
}
