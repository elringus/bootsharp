using System;

namespace DotNetJS
{
    /// <summary>
    /// Applied to a partial method that is expected to be implemented in a global JavaScript function.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public sealed class JSFunctionAttribute : Attribute
    {
        /// <summary>
        /// Gets the identifier for the JavaScript function.
        /// If not set, the identifier is equal to the name of the method with first letter lower-cased.
        /// </summary>
        public string? Identifier { get; }

        /// <summary>
        /// Constructs an instance of <see cref="JSInvokableAttribute"/>.
        /// </summary>
        public JSFunctionAttribute () { }

        /// <summary>
        /// Constructs an instance of <see cref="JSInvokableAttribute"/> using the specified function identifier.
        /// </summary>
        /// <param name="identifier">An identifier for the JavaScript function.</param>
        public JSFunctionAttribute (string identifier)
        {
            if (string.IsNullOrWhiteSpace(identifier))
                throw new ArgumentNullException(nameof(identifier));
            Identifier = identifier;
        }
    }
}
