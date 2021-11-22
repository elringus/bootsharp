using System;

namespace DotNetJS
{
    /// <summary>
    /// Applied to a static non-generic method to allow its invocation from JavaScript.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public sealed class JSInvokableAttribute : Attribute
    {
        /// <summary>
        /// Gets the identifier for the method. The identifier must be unique within the scope
        /// of an assembly. If not set, the identifier is taken from the name of the method. In this case the
        /// method name must be unique within the assembly.
        /// </summary>
        public string? Identifier { get; }

        /// <summary>
        /// Constructs an instance of <see cref="JSInvokableAttribute"/>.
        /// </summary>
        public JSInvokableAttribute () { }

        /// <summary>
        /// Constructs an instance of <see cref="JSInvokableAttribute"/> using the specified method identifier.
        /// </summary>
        /// <param name="identifier">An identifier for the method, which must be unique within the scope of the assembly.</param>
        public JSInvokableAttribute (string identifier)
        {
            if (string.IsNullOrWhiteSpace(identifier))
                throw new ArgumentNullException(nameof(identifier));
            Identifier = identifier;
        }
    }
}
