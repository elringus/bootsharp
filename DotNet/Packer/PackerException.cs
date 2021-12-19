using System;

namespace Packer
{
    public class PackerException : Exception
    {
        public PackerException (string message) : base(message) { }
    }
}
