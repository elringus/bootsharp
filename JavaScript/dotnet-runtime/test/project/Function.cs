using DotNetJS;

namespace Test
{
    public static partial class Function
    {
        [Microsoft.JSInterop.JSInvokable]
        public static string TestEchoFunction (string value)
        {
            return EchoFunction(value);
        }

        [JSFunction]
        private static partial string EchoFunction (string value);
    }
}
