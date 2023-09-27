using Bootsharp;

namespace Test;

public static partial class Functions
{
    [JSInvokable]
    public static string TestEchoFunction (string value) => EchoFunction(value);

    [JSFunction]
    public static partial string EchoFunction (string value);

    [JSInvokable]
    public static string[] TestArrayArgFunction (string[] values) => ArrayArgFunction(values);

    [JSFunction]
    public static partial string[] ArrayArgFunction (string[] values);
}
