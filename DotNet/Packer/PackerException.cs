using System;

namespace DotNetJS.Packer
{
    public class PackerException : Exception
    {
        public PackerException (string message) : base(message) { }
    }
}
