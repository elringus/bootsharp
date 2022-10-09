using System;
using System.Linq;

namespace Packer;

internal class EnumGenerator
{
    private readonly NamespaceBuilder spaceBuilder;

    public EnumGenerator (NamespaceBuilder spaceBuilder)
    {
        this.spaceBuilder = spaceBuilder;
    }

    public string Generate (Type @enum, SpaceObjectBuilder objectBuilder)
    {
        var values = Enum.GetNames(@enum);
        var fields = string.Join(", ", values.Select(v => $"{v}: \"{v}\""));
        var space = spaceBuilder.Build(@enum);
        var js = $"exports.{space}.{@enum.Name} = {{ {fields} }};";
        return objectBuilder.EnsureNamespaceObjectsDeclared(space, js);
    }
}
