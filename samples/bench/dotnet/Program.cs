using System.Runtime.InteropServices.JavaScript;
using System.Text.Json;
using System.Text.Json.Serialization;

public struct Data
{
    public string Info;
    public bool Ok;
    public int Revision;
    public string[] Messages;
}

[JsonSerializable(typeof(Data))]
internal partial class SourceGenerationContext : JsonSerializerContext;

public static partial class Program
{
    public static void Main () { }

    [JSExport]
    public static int EchoNumber () => GetNumber();

    [JSExport]
    public static string EchoStruct ()
    {
        var json = GetStruct();
        var data = JsonSerializer.Deserialize(json, SourceGenerationContext.Default.Data);
        return JsonSerializer.Serialize(data, SourceGenerationContext.Default.Data);
    }

    [JSExport]
    public static int Fi (int n) => n <= 1 ? n : Fi(n - 1) + Fi(n - 2);

    [JSImport("getNumber", "x")]
    private static partial int GetNumber ();

    [JSImport("getStruct", "x")]
    private static partial string GetStruct ();
}
