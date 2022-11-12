namespace Generator
{
    internal static class Common
    {
        public static string MuteNullableWarnings (string source)
        {
            return "\n#nullable enable\n#pragma warning disable\n" +
                   source +
                   "\n#pragma warning restore\n#nullable restore\n";
        }
    }
}
