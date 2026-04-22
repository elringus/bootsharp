using System.Reflection;
using System.Text;
using System.Xml.Linq;

namespace Bootsharp.Publish;

internal sealed class DocumentationBuilder (IReadOnlyCollection<DocumentationMeta> docs)
{
    public string BuildType (Type type, int indent)
    {
        var asm = type.Assembly.GetName().Name!;
        var key = $"T:{GetXmlKey(type)}";
        return GetXml(asm, key) is { } xml ? Build(GetSummary(xml), indent) : "";
    }

    public string BuildProperty (MemberInfo member, int indent)
    {
        var asm = member.DeclaringType!.Assembly.GetName().Name!;
        var key = $"{(member is FieldInfo ? "F" : "P")}:{GetXmlKey(member.DeclaringType!)}.{member.Name}";
        return GetXml(asm, key) is { } xml ? Build(GetSummary(xml), indent) : "";
    }

    public string BuildFunction (MethodMeta method, int indent)
    {
        var asm = method.Info.DeclaringType!.Assembly.GetName().Name!;
        if (GetXml(asm, GetMethodKey()) is not { } xml) return "";

        var sum = GetSummary(xml);
        foreach (var arg in method.Arguments)
            if (xml.Elements("param").FirstOrDefault(e => e.Attribute("name")!.Value == arg.Info.Name) is { } x)
                sum.Add($"@param {arg.JSName} {x.Value}");
        if (xml.Element("returns") is { } returns)
            sum.Add($"@returns {returns.Value}");
        return Build(sum, indent);

        string GetMethodKey ()
        {
            var key = new StringBuilder($"M:{GetXmlKey(method.Info.DeclaringType!)}.{method.Name}");
            var args = method.Info.GetParameters();
            if (args.Length > 0)
                key.Append('(').AppendJoin(',', args.Select(p => GetArgKey(p.ParameterType))).Append(')');
            return key.ToString();
        }

        string GetArgKey (Type type)
        {
            if (type.IsArray) return $"{GetArgKey(type.GetElementType()!)}[{new string(',', type.GetArrayRank() - 1)}]";
            if (!type.IsGenericType) return GetXmlKey(type);
            var definition = type.GetGenericTypeDefinition();
            var name = definition.Name.Split('`')[0];
            name = string.IsNullOrEmpty(definition.Namespace) ? name : $"{definition.Namespace}.{name}";
            return $"{name}{{{string.Join(',', type.GetGenericArguments().Select(GetArgKey))}}}";
        }
    }

    public string BuildEvent (EventMeta @event, int indent)
    {
        return BuildFunction(@event, indent);
    }

    private string Build (IReadOnlyList<string> summary, int indent)
    {
        var pad = new string(' ', indent * 4);
        var builder = new StringBuilder();
        builder.Append($"\n{pad}/**");
        foreach (var line in summary)
            builder.Append($"\n{pad} * {line}");
        builder.Append($"\n{pad} */");
        return builder.ToString();
    }

    private static string GetXmlKey (Type type)
    {
        if (type.IsGenericType) type = type.GetGenericTypeDefinition();
        if (type.IsNested) return $"{GetXmlKey(type.DeclaringType!)}.{type.Name}";
        return string.IsNullOrEmpty(type.Namespace) ? type.Name : $"{type.Namespace}.{type.Name}";
    }

    private XElement? GetXml (string assembly, string key)
    {
        return docs.Where(d => d.Assembly == assembly)
            .SelectMany(d => d.Xml.Descendants("member"))
            .FirstOrDefault(e => e.Attribute("name")!.Value == key);
    }

    private List<string> GetSummary (XElement xml)
    {
        return xml.Elements("summary").Select(e => e.Value).ToList();
    }
}
