using System;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.JavaScript;
using System.Text.Json;
using System.Text.Json.Serialization;

public struct Data
{
    public string Info { get; set; }
    public bool Ok { get; set; }
    public int Revision { get; set; }
    public string[] Messages { get; set; }
}

[JsonSerializable(typeof(Data))]
[JsonSourceGenerationOptions(JsonSerializerDefaults.Web)]
internal partial class SourceGenerationContext : JsonSerializerContext;

public static unsafe partial class Program
{
    public static void Main () { }

    [UnmanagedCallersOnly(EntryPoint = "NativeLibrary_Free")]
    public static void Free (void* p) => NativeMemory.Free(p);

    [UnmanagedCallersOnly(EntryPoint = "echoNumber")]
    public static int EchoNumber () => GetNumber();

    [JSExport]
    public static string EchoStruct ()
    {
        var span = MemoryMarshal.CreateReadOnlySpanFromNullTerminated(GetStruct());
        var data = JsonSerializer.Deserialize(span, SourceGenerationContext.Default.Data);
        return JsonSerializer.Serialize(data, SourceGenerationContext.Default.Data);
    }

    [UnmanagedCallersOnly(EntryPoint = "fi")]
    public static int FiExport (int n) => Fi(n);
    private static int Fi (int n) => n <= 1 ? n : Fi(n - 1) + Fi(n - 2);

    [DllImport("x", EntryPoint = "getNumber")]
    private static extern int GetNumber ();

    [DllImport("x", EntryPoint = "getStruct")]
    private static extern byte* GetStruct ();
}
