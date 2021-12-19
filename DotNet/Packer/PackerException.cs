using System;

namespace Packer
{
    internal class PackerException : Exception
    {
        public PackerException (string message) : base(message) { }
    }
}
