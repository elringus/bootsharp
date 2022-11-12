using Microsoft.CodeAnalysis;

namespace Generator
{
    internal class ExportedType
    {
        private readonly ITypeSymbol type;
        private readonly NamespaceConverter spaceConverter;

        public ExportedType (ITypeSymbol type, NamespaceConverter spaceConverter)
        {
            this.type = type;
            this.spaceConverter = spaceConverter;
        }
    }
}
