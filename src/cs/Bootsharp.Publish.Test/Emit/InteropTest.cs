namespace Bootsharp.Publish.Test;

public class InteropTest : EmitTest
{
    protected override string TestedContent => GeneratedInterop;

    [Fact]
    public void WhenNothingInspectedGeneratesEmptyClass ()
    {
        Execute();
        Contains(
            """
            using System.Runtime.InteropServices.JavaScript;
            using static Bootsharp.Serializer;

            namespace Bootsharp.Generated;

            public static partial class Interop
            {
                [System.Runtime.CompilerServices.ModuleInitializer]
                internal static void RegisterProxies ()
            """);
    }

    [Fact]
    public void GeneratesForMethodsInGlobalSpace ()
    {
        AddAssembly(With(
            """
            public class Class
            {
                [JSInvokable] public static void Inv () {}
                [JSFunction] public static void Fun () {}
                [JSEvent] public static void Evt () {}
            }
            """));
        Execute();
        Contains("""Proxies.Set("Class.Fun", Class_Fun);""");
        Contains("""Proxies.Set("Class.Evt", Class_Evt);""");
        Contains("[System.Runtime.InteropServices.JavaScript.JSExport] internal static void Class_Inv () => global::Class.Inv();");
        Contains("""[System.Runtime.InteropServices.JavaScript.JSImport("Global.Class.funSerialized", "Bootsharp")] internal static partial void Class_Fun ();""");
        Contains("""[System.Runtime.InteropServices.JavaScript.JSImport("Global.Class.evtSerialized", "Bootsharp")] internal static partial void Class_Evt ();""");
    }

    [Fact]
    public void GeneratesForMethodsInCustomSpaces ()
    {
        AddAssembly(With(
            """
            namespace SpaceA
            {
                public class Class
                {
                    [JSInvokable] public static void Inv () {}
                    [JSFunction] public static void Fun () {}
                    [JSEvent] public static void Evt () {}
                }
            }
            namespace SpaceA.SpaceB
            {
                public class Class
                {
                    [JSInvokable] public static void Inv () {}
                    [JSFunction] public static void Fun () {}
                    [JSEvent] public static void Evt () {}
                }
            }
            """));
        Execute();
        Contains("""Proxies.Set("SpaceA.Class.Fun", SpaceA_Class_Fun);""");
        Contains("""Proxies.Set("SpaceA.Class.Evt", SpaceA_Class_Evt);""");
        Contains("""Proxies.Set("SpaceA.SpaceB.Class.Fun", SpaceA_SpaceB_Class_Fun);""");
        Contains("""Proxies.Set("SpaceA.SpaceB.Class.Evt", SpaceA_SpaceB_Class_Evt);""");
        Contains("JSExport] internal static void SpaceA_Class_Inv () => global::SpaceA.Class.Inv();");
        Contains("""JSImport("SpaceA.Class.funSerialized", "Bootsharp")] internal static partial void SpaceA_Class_Fun ();""");
        Contains("""JSImport("SpaceA.Class.evtSerialized", "Bootsharp")] internal static partial void SpaceA_Class_Evt ();""");
        Contains("JSExport] internal static void SpaceA_SpaceB_Class_Inv () => global::SpaceA.SpaceB.Class.Inv();");
        Contains("""JSImport("SpaceA.SpaceB.Class.funSerialized", "Bootsharp")] internal static partial void SpaceA_SpaceB_Class_Fun ();""");
        Contains("""JSImport("SpaceA.SpaceB.Class.evtSerialized", "Bootsharp")] internal static partial void SpaceA_SpaceB_Class_Evt ();""");
    }

    [Fact]
    public void GeneratesForMethodsInGeneratedClasses ()
    {
        AddAssembly(With(
            """
            [assembly:JSExport(typeof(Space.IExport))]
            [assembly:JSImport(typeof(IImport))]

            namespace Space { public interface IExport { void Inv (); } }
            public interface IImport { void Fun (); void NotifyEvt(); }
            """));
        Execute();
        // Contains("""Proxies.Set("Global.Import.Fun", Bootsharp_Generated_Imports_IImport_Fun);""");
        // Contains("""Proxies.Set("Global.Import.NotifyEvt", Bootsharp_Generated_Imports_IImport_OnEvt);""");
        // Contains("JSExport] internal static void Class_Inv () => global::Class.Inv();");
        // Contains("""JSImport("Global.Class.funSerialized", "Bootsharp")] internal static partial void Class_Fun ();""");
        // Contains("""JSImport("Global.Class.evtSerialized", "Bootsharp")] internal static partial void Class_Evt ();""");
    }

    [Fact]
    public void DoesntSerializeTypesThatShouldNotBeSerialized ()
    {
        AddAssembly(With(
            """
            namespace Space;

            public class Class
            {
                [JSInvokable] public static Task<Exception> Inv (bool a1, byte a2, char a3, short a4, long a5, int a6, float a7, double a8, nint a9, DateTime a10, DateTimeOffset a11, string a12, byte[] a13, int[] a14, double[] a15, string[] a16) => default;
                [JSInvokable] public static Task<DateTime> InvNull (bool? a1, byte? a2, char? a3, short? a4, long? a5, int? a6, float? a7, double? a8, nint? a9, DateTime? a10, DateTimeOffset? a11, string? a12, byte?[] a13, int?[] a14, double?[] a15, string?[] a16) => default;
                [JSFunction] public static Task<Exception> Fun (bool a1, byte a2, char a3, short a4, long a5, int a6, float a7, double a8, nint a9, DateTime a10, DateTimeOffset a11, string a12, byte[] a13, int[] a14, double[] a15, string[] a16) => default;
                [JSFunction] public static Task<DateTime> FunNull (bool? a1, byte? a2, char? a3, short? a4, long? a5, int? a6, float? a7, double? a8, nint? a9, DateTime? a10, DateTimeOffset? a11, string? a12, byte?[] a13, int?[] a14, double?[] a15, string?[] a16) => default;
            }
            """));
        Execute();
        Contains("""Proxies.Set("Space.Class.Fun", Space_Class_Fun);""");
        Contains("""Proxies.Set("Space.Class.FunNull", Space_Class_FunNull);""");
        Contains("JSExport] internal static global::System.Threading.Tasks.Task<global::System.Exception> Space_Class_Inv (global::System.Boolean a1, global::System.Byte a2, global::System.Char a3, global::System.Int16 a4, [JSMarshalAs<JSType.BigInt>] global::System.Int64 a5, global::System.Int32 a6, global::System.Single a7, global::System.Double a8, global::System.IntPtr a9, [JSMarshalAs<JSType.Date>] global::System.DateTime a10, [JSMarshalAs<JSType.Date>] global::System.DateTimeOffset a11, global::System.String a12, global::System.Byte[] a13, global::System.Int32[] a14, global::System.Double[] a15, global::System.String[] a16) => global::Space.Class.Inv(a1, a2, a3, a4, a5, a6, a7, a8, a9, a10, a11, a12, a13, a14, a15, a16);");
        Contains("JSExport] [return: JSMarshalAs<JSType.Promise<JSType.Date>>] internal static global::System.Threading.Tasks.Task<global::System.DateTime> Space_Class_InvNull (global::System.Boolean? a1, global::System.Byte? a2, global::System.Char? a3, global::System.Int16? a4, [JSMarshalAs<JSType.BigInt>] global::System.Int64? a5, global::System.Int32? a6, global::System.Single? a7, global::System.Double? a8, global::System.IntPtr? a9, [JSMarshalAs<JSType.Date>] global::System.DateTime? a10, [JSMarshalAs<JSType.Date>] global::System.DateTimeOffset? a11, global::System.String? a12, global::System.Byte?[] a13, global::System.Int32?[] a14, global::System.Double?[] a15, global::System.String?[] a16) => global::Space.Class.InvNull(a1, a2, a3, a4, a5, a6, a7, a8, a9, a10, a11, a12, a13, a14, a15, a16);");
        Contains("""JSImport("Space.Class.funSerialized", "Bootsharp")] internal static partial global::System.Threading.Tasks.Task<global::System.Exception> Space_Class_Fun (global::System.Boolean a1, global::System.Byte a2, global::System.Char a3, global::System.Int16 a4, [JSMarshalAs<JSType.BigInt>] global::System.Int64 a5, global::System.Int32 a6, global::System.Single a7, global::System.Double a8, global::System.IntPtr a9, [JSMarshalAs<JSType.Date>] global::System.DateTime a10, [JSMarshalAs<JSType.Date>] global::System.DateTimeOffset a11, global::System.String a12, global::System.Byte[] a13, global::System.Int32[] a14, global::System.Double[] a15, global::System.String[] a16);""");
        Contains("""JSImport("Space.Class.funNullSerialized", "Bootsharp")] [return: JSMarshalAs<JSType.Promise<JSType.Date>>] internal static partial global::System.Threading.Tasks.Task<global::System.DateTime> Space_Class_FunNull (global::System.Boolean? a1, global::System.Byte? a2, global::System.Char? a3, global::System.Int16? a4, [JSMarshalAs<JSType.BigInt>] global::System.Int64? a5, global::System.Int32? a6, global::System.Single? a7, global::System.Double? a8, global::System.IntPtr? a9, [JSMarshalAs<JSType.Date>] global::System.DateTime? a10, [JSMarshalAs<JSType.Date>] global::System.DateTimeOffset? a11, global::System.String? a12, global::System.Byte?[] a13, global::System.Int32?[] a14, global::System.Double?[] a15, global::System.String?[] a16);""");
    }

    [Fact]
    public void SerializesTypesThatShouldBeSerialized ()
    {
        AddAssembly(With(
            """
            namespace Space;

            public record Record;

            public class Class
            {
                [JSInvokable] public static Record InvA (Record a) => default;
                [JSInvokable] public static Task<Record?[]?> InvB (Record?[]? a) => default;
                [JSFunction] public static Record FunA (Record a) => Proxies.Get<Func<Record, Record>>("Space.Class.FunA")(a);
                [JSFunction] public static Task<Record?[]?> FunB (Record?[]? a) => Proxies.Get<Func<Record?[]?, Task<Record?[]?>>>("Space.Class.FunB")(a);

                [JSInvokable] public static Task<byte[]> InvAsyncBytes () => default;
                [JSFunction] public static Task<byte[]> FunAsyncBytes () => Proxies.Get<Func<Task<byte[]>>>("Space.Class.funAsyncBytes")();
            }
            """));
        Execute();
        // TODO: Proxies.Set sets de-serialized lambda wrapper over the interop method, so that we can Proxies.Get on the other side w/o additional processing.
        Contains("""Proxies.Set("Space.Class.FunA", Space_Class_FunA);""");
        Contains("""Proxies.Set("Space.Class.FunB", Space_Class_FunB);""");
        Contains("JSExport] internal static global::System.String Space_Class_InvA (global::System.String a) => Serialize(global::Space.Class.InvA(Deserialize<global::Space.Record>(a)));");
        Contains("JSExport] internal static async global::System.Threading.Tasks.Task<global::System.String?> Space_Class_InvB (global::System.String? a) => Serialize(await global::Space.Class.InvB(Deserialize<global::Space.Record?[]?>(a)));");
        Contains("""JSImport("Space.Class.funASerialized", "Bootsharp")] internal static partial global::System.String Space_Class_FunA (global::System.String a);""");
        Contains("""JSImport("Space.Class.funBSerialized", "Bootsharp")] internal static partial global::System.Threading.Tasks.Task<global::System.String?> Space_Class_FunB (global::System.String? a);""");

        // TODO: Remove when resolved: https://github.com/elringus/bootsharp/issues/138
        Contains("""Proxies.Set("Space.Class.FunAsyncBytes", Space_Class_FunAsyncBytes);""");
        Contains("JSExport] internal static async global::System.Threading.Tasks.Task<global::System.String> Space_Class_InvAsyncBytes () => Serialize(await global::Space.Class.InvAsyncBytes());");
        Contains("""JSImport("Space.Class.funAsyncBytesSerialized", "Bootsharp")] internal static partial global::System.Threading.Tasks.Task<global::System.String> Space_Class_FunAsyncBytes ();""");
    }
}
