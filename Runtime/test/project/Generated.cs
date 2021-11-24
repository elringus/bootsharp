using DotNetJS;

namespace Test
{
    public static partial class Generated
    {
        [Microsoft.JSInterop.JSInvokable]
        public static string TestEchoGenerated (string value)
        {
            return EchoGenerated(value);
        }

        [JSFunction]
        private static partial string EchoGenerated (string value);
    }
}
