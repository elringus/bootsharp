using Microsoft.JSInterop;

namespace Test.Project;

public static class Program
{
    private static bool mainInvoked;

    private static void Main ()
    {
        mainInvoked = true;
    }

    [JSInvokable]
    public static bool IsMainInvoked () => mainInvoked;
}
