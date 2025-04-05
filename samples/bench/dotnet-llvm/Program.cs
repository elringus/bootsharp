using System.Runtime.InteropServices;
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

public static unsafe class Program
{
    public static void Main () { }

    [UnmanagedCallersOnly(EntryPoint = "NativeLibrary_Free")]
    public static void Free (void* p) => NativeMemory.Free(p);

    [UnmanagedCallersOnly(EntryPoint = "echoNumber")]
    public static int EchoNumber () => GetNumber();

    [UnmanagedCallersOnly(EntryPoint = "echoStruct")]
    public static char* EchoStruct ()
    {
        var span = MemoryMarshal.CreateReadOnlySpanFromNullTerminated(GetStruct());
        var data = JsonSerializer.Deserialize(span, SourceGenerationContext.Default.Data);
        var json = JsonSerializer.Serialize(data, SourceGenerationContext.Default.Data);
        fixed (char* ptr = json) return ptr; // has to be pinned and freed after use in real use cases
    }

    [UnmanagedCallersOnly(EntryPoint = "fi")]
    public static int FiExport (int n) => Fi(n);
    private static int Fi (int n) => n <= 1 ? n : Fi(n - 1) + Fi(n - 2);

    [DllImport("x", EntryPoint = "getNumber")]
    private static extern int GetNumber ();

    [DllImport("x", EntryPoint = "getStruct")]
    private static extern char* GetStruct ();
}

// NOTE: 95% of degradation compared to Rust is in the JSON de-/serialization.
// GenerationMode = JsonSourceGenerationMode.Serialization is only implemented for serialization
// and throws when used for de-serialization: https://github.com/dotnet/runtime/issues/55043.
