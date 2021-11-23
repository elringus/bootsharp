using System.Collections.Generic;

namespace DotNetJS.Generator
{
    internal static class Log
    {
        private static readonly List<string> messages = new();

        public static void Add (string message) => messages.Add(message);

        public static string Flush ()
        {
            var result = $"/*\n{string.Join('\n', messages)}\n*/";
            messages.Clear();
            return result;
        }
    }
}
