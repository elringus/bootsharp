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
    public void CanGenerateForMethodsInGlobalSpace ()
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
        Contains("""Proxies.Set("Global.Class.fun", Class_Fun);""");
        Contains("""Proxies.Set("Global.Class.evt", Class_Evt);""");
        Contains("[System.Runtime.InteropServices.JavaScript.JSExport] internal static void Class_Inv () => global::Class.Inv();");
        Contains("""[System.Runtime.InteropServices.JavaScript.JSImport("Global.Class.funSerialized", "Bootsharp")] internal static partial void Class_Fun ();""");
        Contains("""[System.Runtime.InteropServices.JavaScript.JSImport("Global.Class.evtSerialized", "Bootsharp")] internal static partial void Class_Evt ();""");
    }

    [Fact]
    public void CanGenerateForMethodsInCustomSpaces ()
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
        Contains("""Proxies.Set("SpaceA.Class.fun", SpaceA_Class_Fun);""");
        Contains("""Proxies.Set("SpaceA.Class.evt", SpaceA_Class_Evt);""");
        Contains("""Proxies.Set("SpaceA.SpaceB.Class.fun", SpaceA_SpaceB_Class_Fun);""");
        Contains("""Proxies.Set("SpaceA.SpaceB.Class.evt", SpaceA_SpaceB_Class_Evt);""");
        Contains("JSExport] internal static void SpaceA_Class_Inv () => global::SpaceA.Class.Inv();");
        Contains("""JSImport("SpaceA.Class.funSerialized", "Bootsharp")] internal static partial void SpaceA_Class_Fun ();""");
        Contains("""JSImport("SpaceA.Class.evtSerialized", "Bootsharp")] internal static partial void SpaceA_Class_Evt ();""");
        Contains("JSExport] internal static void SpaceA_SpaceB_Class_Inv () => global::SpaceA.SpaceB.Class.Inv();");
        Contains("""JSImport("SpaceA.SpaceB.Class.funSerialized", "Bootsharp")] internal static partial void SpaceA_SpaceB_Class_Fun ();""");
        Contains("""JSImport("SpaceA.SpaceB.Class.evtSerialized", "Bootsharp")] internal static partial void SpaceA_SpaceB_Class_Evt ();""");
    }

    [Fact]
    public void HandlesVariousArgumentAndReturnTypes ()
    {
        AddAssembly(With(
            """
            namespace Space;

            public record Info;

            public class Class
            {
                [JSInvokable] public static void InvVoid () {}
                [JSInvokable] public static Info InvWithArgs (Info a, int[] b) => default;
                [JSInvokable] public static Task InvAsync () => default;
                [JSInvokable] public static Task<Info?> InvAsyncWithArgs (Info? i) => default;
                [JSFunction] public static Info Fun (string a, int[] b) => default;
                [JSFunction] public static Task FunAsync () => default;
                [JSFunction] public static Task<Info?> FunAsyncWithArgs (Info a) => default;
                [JSEvent] public static void EvtWithArgs (Info? a, bool? b) {}
            }
            """));
        Execute();
        Contains("JSExport] internal static void Space_Class_InvVoid () => global::Space.Class.InvVoid();");
        Contains("JSExport] internal static global::System.String Space_Class_InvWithArgs (global::System.String a, global::System.Int32[] b) => Serialize(global::Space.Class.InvWithArgs(Deserialize<global::Space.Info>(a), b));");
        Contains("JSExport] internal static global::System.Threading.Tasks.Task Space_Class_InvAsync () => global::Space.Class.InvAsync();");
        Contains("JSExport] internal static async global::System.Threading.Tasks.Task<global::System.String?> Space_Class_InvAsyncWithArgs (global::System.String? i) => Serialize(await global::Space.Class.InvAsyncWithArgs(Deserialize<global::Space.Info?>(i)));");
        Contains("""JSImport("Space.Class.funSerialized", "Bootsharp")] internal static partial global::System.String Space_Class_Fun (global::System.String a, global::System.Int32[] b);""");
        Contains("""JSImport("Space.Class.funAsyncSerialized", "Bootsharp")] internal static partial global::System.Threading.Tasks.Task Space_Class_FunAsync ();""");
        Contains("""JSImport("Space.Class.funAsyncWithArgsSerialized", "Bootsharp")] internal static partial global::System.Threading.Tasks.Task<global::System.String?> Space_Class_FunAsyncWithArgs (global::System.String a);""");
        Contains("""JSImport("Space.Class.evtWithArgsSerialized", "Bootsharp")] internal static partial void Space_Class_EvtWithArgs (global::System.String? a, global::System.Boolean? b);""");
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
            }
            """));
        Execute();
        Contains("JSExport] internal static global::System.Threading.Tasks.Task<global::System.Exception> Space_Class_Inv (global::System.Boolean a1, global::System.Byte a2, global::System.Char a3, global::System.Int16 a4, [JSMarshalAs<JSType.BigInt>] global::System.Int64 a5, global::System.Int32 a6, global::System.Single a7, global::System.Double a8, global::System.IntPtr a9, [JSMarshalAs<JSType.Date>] global::System.DateTime a10, [JSMarshalAs<JSType.Date>] global::System.DateTimeOffset a11, global::System.String a12, global::System.Byte[] a13, global::System.Int32[] a14, global::System.Double[] a15, global::System.String[] a16) => global::Space.Class.Inv(a1, a2, a3, a4, a5, a6, a7, a8, a9, a10, a11, a12, a13, a14, a15, a16);");
        Contains("JSExport] [return: JSMarshalAs<JSType.Promise<JSType.Date>>] internal static global::System.Threading.Tasks.Task<global::System.DateTime> Space_Class_InvNull (global::System.Boolean? a1, global::System.Byte? a2, global::System.Char? a3, global::System.Int16? a4, [JSMarshalAs<JSType.BigInt>] global::System.Int64? a5, global::System.Int32? a6, global::System.Single? a7, global::System.Double? a8, global::System.IntPtr? a9, [JSMarshalAs<JSType.Date>] global::System.DateTime? a10, [JSMarshalAs<JSType.Date>] global::System.DateTimeOffset? a11, global::System.String? a12, global::System.Byte?[] a13, global::System.Int32?[] a14, global::System.Double?[] a15, global::System.String?[] a16) => global::Space.Class.InvNull(a1, a2, a3, a4, a5, a6, a7, a8, a9, a10, a11, a12, a13, a14, a15, a16);");
    }

    [Fact]
    public void SerializesTypesThatShouldBeSerialized ()
    {
        AddAssembly(With(
            """
            namespace Space;

            public record Info;

            public class Class
            {
                [JSInvokable] public static Task<Info[]> InvA () => default;
                [JSInvokable] public static Task<byte[]> InvB () => default;
            }
            """));
        Execute();
        Contains("JSExport] internal static async global::System.Threading.Tasks.Task<global::System.String> Space_Class_InvA () => Serialize(await global::Space.Class.InvA());");
        Contains("JSExport] internal static async global::System.Threading.Tasks.Task<global::System.String> Space_Class_InvB () => Serialize(await global::Space.Class.InvB());");
    }
}
