namespace Bootsharp.Publish;

internal sealed class BindingMarshaler
{
    private readonly Dictionary<string, string> generatedByName = [];

    public string Marshal (Type type)
    {
        if (IsTaskWithResult(type, out var result)) type = result;
        if (ShouldMarshalPassThrough(type)) return "";
        var fnName = $"marshal_{GetMarshalId(type)}";
        if (generatedByName.ContainsKey(fnName)) return fnName;
        generatedByName[fnName] = GenerateMarshalFunction(fnName, type);
        return fnName;
    }

    public string Unmarshal (Type type)
    {
        if (IsTaskWithResult(type, out var result)) type = result;
        if (ShouldMarshalPassThrough(type)) return "";
        var fnName = $"unmarshal_{GetMarshalId(type)}";
        if (generatedByName.ContainsKey(fnName)) return fnName;
        generatedByName[fnName] = GenerateUnmarshalFunction(fnName, type);
        return fnName;
    }

    public IReadOnlyCollection<string> GetGenerated () => [];

    private string GenerateMarshalFunction (string fnName, Type marshaledType)
    {
        return $"function {fnName} (obj) {{ return ...; }}";
    }

    private string GenerateUnmarshalFunction (string fnName, Type marshaledType)
    {
        return $"function {fnName} (raw) {{ return ...; }}";
    }
}
