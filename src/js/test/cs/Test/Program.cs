using Bootsharp;

namespace Test;

public static partial class Program
{
    public static void Main () => OnMainInvoked();

    [JSFunction]
    public static partial void OnMainInvoked ();
}
