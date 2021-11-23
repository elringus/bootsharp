namespace DotNetJS.Generator
{
    internal static class Attributes
    {
        public static readonly string JSFunction = nameof(JSFunctionAttribute)[..^9];
        public static readonly string JSInvokable = nameof(JSInvokableAttribute)[..^9];
    }
}
