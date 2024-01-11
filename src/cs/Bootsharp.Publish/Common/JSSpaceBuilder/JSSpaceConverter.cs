using System.Reflection;
using System.Text.RegularExpressions;

namespace Bootsharp.Publish;

internal sealed class JSSpaceConverter (CustomAttributeData attribute)
{
    private readonly string pattern = (string)attribute.ConstructorArguments[0].Value!;
    private readonly string replacement = (string)attribute.ConstructorArguments[1].Value!;

    public string Convert (string space) => Regex.Replace(space, pattern, replacement);
}
