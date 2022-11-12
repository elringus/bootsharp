using Microsoft.CodeAnalysis;
using static Generator.Common;

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

        public string EmitSource ()
        {
            return MuteNullableWarnings(
                EmitImport() +
                WrapNamespace(
                    EmitHeader() +
                    EmitMethods(compilation) +
                    EmitFooter()
                )
            );
        }
    }
}
