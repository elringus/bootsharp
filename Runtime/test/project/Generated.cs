using DotNetJS;
using System;
using System.Threading.Tasks;

namespace Test
{
    public static partial class Generated
    {
        [JSInvokable]
        public static void TestEchoGenerated (string value)
        {
            var echo = EchoGenerated(value);
            if (value != echo)
                throw new Exception($"Expected '{value}', but received '{echo}'.");
        }

        [JSInvokable]
        public static async Task TestEchoGeneratedAsync (string value)
        {
            var echo = await EchoGeneratedAsync(value);
            if (value != echo)
                throw new Exception($"Expected '{value}', but received '{echo}'.");
        }

        [JSFunction]
        public static partial string EchoGenerated (string value);

        [JSFunction]
        public static partial ValueTask<string> EchoGeneratedAsync (string value);
    }
}
