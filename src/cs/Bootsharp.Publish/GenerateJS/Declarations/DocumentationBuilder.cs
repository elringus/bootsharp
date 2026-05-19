using System.Reflection;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace Bootsharp.Publish;

internal sealed class DocumentationBuilder
{
    private readonly Dictionary<(string Assembly, string Key), XElement> xmlByKey = [];
    private readonly CodeBuilder bld;

    public DocumentationBuilder (CodeBuilder bld, IReadOnlyCollection<DocMeta> docs)
    {
        this.bld = bld;
        foreach (var doc in docs)
        foreach (var member in doc.Xml.Descendants("member"))
            if (member.Attribute("name") is { } name)
                xmlByKey.TryAdd((doc.Assembly, name.Value), member);
    }

    public void Type (TypeMeta type)
    {
        var asm = type.Clr.Assembly.GetName().Name!;
        var key = $"T:{GetXmlKey(type.Clr)}";
        if (GetXml(asm, key) is { } xml)
            Append(GetSummary(xml));
    }

    public void Event (EventMeta evt)
    {
        var asm = evt.Info.DeclaringType!.Assembly.GetName().Name!;
        var key = $"E:{GetXmlKey(evt.Info.DeclaringType!)}.{evt.Name}";
        if (GetXml(asm, key) is not { } xml) return;

        var sum = GetSummary(xml);
        foreach (var arg in evt.Args)
            if ((GetArgXml(xml, arg) ?? GetDelegateArgXml(arg)) is { } x)
                sum.Add($"@param {arg.JSName} {RenderXml(x)}");
        Append(sum);

        XElement? GetDelegateArgXml (ArgumentMeta arg)
        {
            var asm = evt.Info.EventHandlerType!.Assembly.GetName().Name!;
            var key = $"T:{GetXmlKey(evt.Info.EventHandlerType!)}";
            return GetXml(asm, key) is { } x ? GetArgXml(x, arg) : null;
        }
    }

    public void Property (MemberInfo member)
    {
        var asm = member.DeclaringType!.Assembly.GetName().Name!;
        var key = $"{(member is FieldInfo ? "F" : "P")}:{GetXmlKey(member.DeclaringType!)}.{member.Name}";
        if (GetXml(asm, key) is { } xml)
            Append(GetSummary(xml));
    }

    public void Method (MethodMeta method)
    {
        var asm = method.Info.DeclaringType!.Assembly.GetName().Name!;
        if (GetXml(asm, GetMethodKey(method)) is not { } xml) return;

        var sum = GetSummary(xml);
        foreach (var arg in method.Args)
            if (GetArgXml(xml, arg) is { } x)
                sum.Add($"@param {arg.JSName} {RenderXml(x)}");
        if (xml.Element("returns") is { } returns)
            sum.Add($"@returns {RenderXml(returns)}");
        Append(sum);

        static string GetMethodKey (MethodMeta method)
        {
            var bld = new CodeBuilder($"M:{GetXmlKey(method.Info.DeclaringType!)}.{method.Name}");
            var args = method.Info.GetParameters();
            if (args.Length > 0)
                bld.Append("(").Join(",", args.Select(p => GetArgKey(p.ParameterType))).Append(")");
            return bld.ToString();
        }

        static string GetArgKey (Type type)
        {
            if (type.IsArray) return $"{GetArgKey(type.GetElementType()!)}[{new string(',', type.GetArrayRank() - 1)}]";
            if (!type.IsGenericType) return GetXmlKey(type);
            var definition = type.GetGenericTypeDefinition();
            var name = definition.Name.Split('`')[0];
            name = string.IsNullOrEmpty(definition.Namespace) ? name : $"{definition.Namespace}.{name}";
            return $"{name}{{{string.Join(',', type.GetGenericArguments().Select(GetArgKey))}}}";
        }
    }

    private void Append (IReadOnlyList<string> summary)
    {
        bld.Line("/**");
        foreach (var line in summary)
            bld.Line($" * {line}");
        bld.Line(" */");
    }

    private static string GetXmlKey (Type type)
    {
        if (type.IsGenericType) type = type.GetGenericTypeDefinition();
        if (type.IsNested) return $"{GetXmlKey(type.DeclaringType!)}.{type.Name}";
        return string.IsNullOrEmpty(type.Namespace) ? type.Name : $"{type.Namespace}.{type.Name}";
    }

    private XElement? GetXml (string assembly, string key)
    {
        return xmlByKey.GetValueOrDefault((assembly, key));
    }

    private static XElement? GetArgXml (XElement xml, ArgumentMeta arg)
    {
        return xml.Elements("param").FirstOrDefault(e => e.Attribute("name")!.Value == arg.Info.Name);
    }

    private static List<string> GetSummary (XElement xml)
    {
        return xml.Elements("summary").Select(RenderXml).ToList();
    }

    private static string RenderXml (XElement xml) =>
        Regex.Replace(string.Concat(xml.DescendantNodes().Select(node => node switch {
            XText xt => xt.Value,
            XElement xe when xe.Attribute("cref")?.Value is { } cref =>
                Regex.Replace(Regex.Replace(cref, "[`(].*$", ""), "^.*[.:]", ""),
            XElement xe when (xe.Attribute("langword") ?? xe.Attribute("name"))?.Value is { } v => v,
            _ => ""
        })), @"\s+", " ").Trim();
}
