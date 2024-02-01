namespace Bootsharp.Publish.Test;

public class InteropTest : EmitTest
{
    protected override string TestedContent => GeneratedInterop;

    [Fact]
    public void WhenNothingInspectedGeneratesDefaults ()
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
    public void GeneratesDisposeInstanceBindings ()
    {
        Execute();
        Contains("JSExport] internal static void DisposeExportedInstance (global::System.Int32 id) => global::Bootsharp.Instances.Dispose(id);");
        Contains("""JSImport("disposeInstance", "Bootsharp")] internal static partial void DisposeImportedInstance (global::System.Int32 id);""");
    }

    [Fact]
    public void GeneratesForMethodsWithoutNamespace ()
    {
        AddAssembly(With(
            """
            public class Class
            {
                [JSInvokable] public static void Inv () {}
                [JSFunction] public static void Fun () => Proxies.Get<Action>("Class.Fun")();
                [JSEvent] public static void Evt () => Proxies.Get<Action>("Class.Evt")();
            }
            """));
        Execute();
        Contains("""Proxies.Set("Class.Fun", () => Class_Fun());""");
        Contains("""Proxies.Set("Class.Evt", () => Class_Evt());""");
        Contains("JSExport] internal static void Class_Inv () => global::Class.Inv();");
        Contains("""JSImport("Class.funSerialized", "Bootsharp")] internal static partial void Class_Fun ();""");
        Contains("""JSImport("Class.evtSerialized", "Bootsharp")] internal static partial void Class_Evt ();""");
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
                    [JSFunction] public static void Fun () => Proxies.Get<Action>("SpaceA.Class.Fun")();
                    [JSEvent] public static void Evt () => Proxies.Get<Action>("SpaceA.Class.Evt")();
                }
            }
            namespace SpaceA.SpaceB
            {
                public class Class
                {
                    [JSInvokable] public static void Inv () {}
                    [JSFunction] public static void Fun () => Proxies.Get<Action>("SpaceA.SpaceB.Class.Fun")();
                    [JSEvent] public static void Evt () => Proxies.Get<Action>("SpaceA.SpaceB.Class.Evt")();
                }
            }
            """));
        Execute();
        Contains("""Proxies.Set("SpaceA.Class.Fun", () => SpaceA_Class_Fun());""");
        Contains("""Proxies.Set("SpaceA.Class.Evt", () => SpaceA_Class_Evt());""");
        Contains("""Proxies.Set("SpaceA.SpaceB.Class.Fun", () => SpaceA_SpaceB_Class_Fun());""");
        Contains("""Proxies.Set("SpaceA.SpaceB.Class.Evt", () => SpaceA_SpaceB_Class_Evt());""");
        Contains("JSExport] internal static void SpaceA_Class_Inv () => global::SpaceA.Class.Inv();");
        Contains("""JSImport("SpaceA.Class.funSerialized", "Bootsharp")] internal static partial void SpaceA_Class_Fun ();""");
        Contains("""JSImport("SpaceA.Class.evtSerialized", "Bootsharp")] internal static partial void SpaceA_Class_Evt ();""");
        Contains("JSExport] internal static void SpaceA_SpaceB_Class_Inv () => global::SpaceA.SpaceB.Class.Inv();");
        Contains("""JSImport("SpaceA.SpaceB.Class.funSerialized", "Bootsharp")] internal static partial void SpaceA_SpaceB_Class_Fun ();""");
        Contains("""JSImport("SpaceA.SpaceB.Class.evtSerialized", "Bootsharp")] internal static partial void SpaceA_SpaceB_Class_Evt ();""");
    }

    [Fact]
    public void GeneratesForStaticInteropInterfaces ()
    {
        AddAssembly(With(
            """
            [assembly:JSExport(typeof(Space.IExported))]
            [assembly:JSImport(typeof(IImported))]

            namespace Space { public interface IExported { void Inv (); } }
            public interface IImported { void Fun (); void NotifyEvt(); }
            """));
        Execute();
        Contains("""Proxies.Set("Bootsharp.Generated.Imports.JSImported.Fun", () => Bootsharp_Generated_Imports_JSImported_Fun());""");
        Contains("""Proxies.Set("Bootsharp.Generated.Imports.JSImported.OnEvt", () => Bootsharp_Generated_Imports_JSImported_OnEvt());""");
        Contains("JSExport] internal static void Bootsharp_Generated_Exports_Space_JSExported_Inv () => global::Bootsharp.Generated.Exports.Space.JSExported.Inv();");
        Contains("""JSImport("Imported.funSerialized", "Bootsharp")] internal static partial void Bootsharp_Generated_Imports_JSImported_Fun ();""");
        Contains("""JSImport("Imported.onEvtSerialized", "Bootsharp")] internal static partial void Bootsharp_Generated_Imports_JSImported_OnEvt ();""");
    }

    [Fact]
    public void GeneratesForInstancedInteropInterfaces ()
    {
        AddAssembly(With(
            """
            namespace Space
            {
                public interface IExported { void Inv (); }
                public interface IImported { void Fun (); }
            }

            public interface IExported { void Inv (); }
            public interface IImported { void NotifyEvt(); }

            public class Class
            {
                [JSInvokable] public static Space.IExported GetExported (Space.IImported arg) => default;
                [JSFunction] public static IImported GetImported (IExported arg) => Proxies.Get<Func<IExported, IImported>>("Class.GetImported")(arg);
            }
            """));
        Execute();
        Contains("""Proxies.Set("Class.GetImported", (global::IExported arg) => new global::Bootsharp.Generated.Imports.JSImported(Class_GetImported(global::Bootsharp.Instances.Register(arg))));""");
        Contains("""Proxies.Set("Bootsharp.Generated.Imports.JSImported.OnEvt", (global::System.Int32 _id) => Bootsharp_Generated_Imports_JSImported_OnEvt(_id));""");
        Contains("""Proxies.Set("Bootsharp.Generated.Imports.Space.JSImported.Fun", (global::System.Int32 _id) => Bootsharp_Generated_Imports_Space_JSImported_Fun(_id));""");
        Contains("JSExport] internal static global::System.Int32 Class_GetExported (global::System.Int32 arg) => global::Bootsharp.Instances.Register(global::Class.GetExported(new global::Bootsharp.Generated.Imports.Space.JSImported(arg)));");
        Contains("""JSImport("Class.getImportedSerialized", "Bootsharp")] internal static partial global::System.Int32 Class_GetImported (global::System.Int32 arg);""");
        Contains("JSExport] internal static void Bootsharp_Generated_Exports_JSExported_Inv (global::System.Int32 _id) => ((global::IExported)global::Bootsharp.Instances.Get(_id)).Inv();");
        Contains("""JSImport("Imported.onEvtSerialized", "Bootsharp")] internal static partial void Bootsharp_Generated_Imports_JSImported_OnEvt (global::System.Int32 _id);""");
        Contains("JSExport] internal static void Bootsharp_Generated_Exports_Space_JSExported_Inv (global::System.Int32 _id) => ((global::Space.IExported)global::Bootsharp.Instances.Get(_id)).Inv();");
        Contains("""JSImport("Space.Imported.funSerialized", "Bootsharp")] internal static partial void Bootsharp_Generated_Imports_Space_JSImported_Fun (global::System.Int32 _id);""");
    }

    [Fact]
    public void DoesntSerializeTypesThatShouldNotBeSerialized ()
    {
        AddAssembly(With(
            """
            namespace Space;

            public class Class
            {
                [JSInvokable] public static Task<Exception> Inv (bool a1, byte a2, char a3, short a4, long a5, int a6, float a7, double a8, nint a9, DateTime a10, DateTimeOffset a11, string a12, byte[] a13, int[] a14, double[] a15, string[] a16) => default!;
                [JSInvokable] public static Task<DateTime> InvNull (bool? a1, byte? a2, char? a3, short? a4, long? a5, int? a6, float? a7, double? a8, nint? a9, DateTime? a10, DateTimeOffset? a11, string? a12, byte[]? a13, int[]? a14, double[]? a15, string[]? a16) => default!;
                [JSFunction] public static Task<Exception> Fun (bool a1, byte a2, char a3, short a4, long a5, int a6, float a7, double a8, nint a9, DateTime a10, DateTimeOffset a11, string a12, byte[] a13, int[] a14, double[] a15, string[] a16) => Proxies.Get<Func<bool, byte, char, short, long, int, float, double, nint, DateTime, DateTimeOffset, string, byte[], int[], double[], string[], Task<Exception>>>("Space.Class.Fun")(a1, a2, a3, a4, a5, a6, a7, a8, a9, a10, a11, a12, a13, a14, a15, a16);
                [JSFunction] public static Task<DateTime> FunNull (bool? a1, byte? a2, char? a3, short? a4, long? a5, int? a6, float? a7, double? a8, nint? a9, DateTime? a10, DateTimeOffset? a11, string? a12, byte[]? a13, int[]? a14, double[]? a15, string[]? a16) => Proxies.Get<Func<bool?, byte?, char?, short?, long?, int?, float?, double?, nint?, DateTime?, DateTimeOffset?, string?, byte[]?, int[]?, double[]?, string[]?, Task<DateTime>>>("Space.Class.FunNull")(a1, a2, a3, a4, a5, a6, a7, a8, a9, a10, a11, a12, a13, a14, a15, a16);
            }
            """));
        Execute();
        Contains("""Proxies.Set("Space.Class.Fun", (global::System.Boolean a1, global::System.Byte a2, global::System.Char a3, global::System.Int16 a4, global::System.Int64 a5, global::System.Int32 a6, global::System.Single a7, global::System.Double a8, global::System.IntPtr a9, global::System.DateTime a10, global::System.DateTimeOffset a11, global::System.String a12, global::System.Byte[] a13, global::System.Int32[] a14, global::System.Double[] a15, global::System.String[] a16) => Space_Class_Fun(a1, a2, a3, a4, a5, a6, a7, a8, a9, a10, a11, a12, a13, a14, a15, a16));""");
        Contains("""Proxies.Set("Space.Class.FunNull", (global::System.Boolean? a1, global::System.Byte? a2, global::System.Char? a3, global::System.Int16? a4, global::System.Int64? a5, global::System.Int32? a6, global::System.Single? a7, global::System.Double? a8, global::System.IntPtr? a9, global::System.DateTime? a10, global::System.DateTimeOffset? a11, global::System.String? a12, global::System.Byte[]? a13, global::System.Int32[]? a14, global::System.Double[]? a15, global::System.String[]? a16) => Space_Class_FunNull(a1, a2, a3, a4, a5, a6, a7, a8, a9, a10, a11, a12, a13, a14, a15, a16));""");
        Contains("JSExport] internal static global::System.Threading.Tasks.Task<global::System.Exception> Space_Class_Inv (global::System.Boolean a1, global::System.Byte a2, global::System.Char a3, global::System.Int16 a4, [JSMarshalAs<JSType.BigInt>] global::System.Int64 a5, global::System.Int32 a6, global::System.Single a7, global::System.Double a8, global::System.IntPtr a9, [JSMarshalAs<JSType.Date>] global::System.DateTime a10, [JSMarshalAs<JSType.Date>] global::System.DateTimeOffset a11, global::System.String a12, global::System.Byte[] a13, global::System.Int32[] a14, global::System.Double[] a15, global::System.String[] a16) => global::Space.Class.Inv(a1, a2, a3, a4, a5, a6, a7, a8, a9, a10, a11, a12, a13, a14, a15, a16);");
        Contains("JSExport] [return: JSMarshalAs<JSType.Promise<JSType.Date>>] internal static global::System.Threading.Tasks.Task<global::System.DateTime> Space_Class_InvNull (global::System.Boolean? a1, global::System.Byte? a2, global::System.Char? a3, global::System.Int16? a4, [JSMarshalAs<JSType.BigInt>] global::System.Int64? a5, global::System.Int32? a6, global::System.Single? a7, global::System.Double? a8, global::System.IntPtr? a9, [JSMarshalAs<JSType.Date>] global::System.DateTime? a10, [JSMarshalAs<JSType.Date>] global::System.DateTimeOffset? a11, global::System.String? a12, global::System.Byte[]? a13, global::System.Int32[]? a14, global::System.Double[]? a15, global::System.String[]? a16) => global::Space.Class.InvNull(a1, a2, a3, a4, a5, a6, a7, a8, a9, a10, a11, a12, a13, a14, a15, a16);");
        Contains("""JSImport("Space.Class.funSerialized", "Bootsharp")] internal static partial global::System.Threading.Tasks.Task<global::System.Exception> Space_Class_Fun (global::System.Boolean a1, global::System.Byte a2, global::System.Char a3, global::System.Int16 a4, [JSMarshalAs<JSType.BigInt>] global::System.Int64 a5, global::System.Int32 a6, global::System.Single a7, global::System.Double a8, global::System.IntPtr a9, [JSMarshalAs<JSType.Date>] global::System.DateTime a10, [JSMarshalAs<JSType.Date>] global::System.DateTimeOffset a11, global::System.String a12, global::System.Byte[] a13, global::System.Int32[] a14, global::System.Double[] a15, global::System.String[] a16);""");
        Contains("""JSImport("Space.Class.funNullSerialized", "Bootsharp")] [return: JSMarshalAs<JSType.Promise<JSType.Date>>] internal static partial global::System.Threading.Tasks.Task<global::System.DateTime> Space_Class_FunNull (global::System.Boolean? a1, global::System.Byte? a2, global::System.Char? a3, global::System.Int16? a4, [JSMarshalAs<JSType.BigInt>] global::System.Int64? a5, global::System.Int32? a6, global::System.Single? a7, global::System.Double? a8, global::System.IntPtr? a9, [JSMarshalAs<JSType.Date>] global::System.DateTime? a10, [JSMarshalAs<JSType.Date>] global::System.DateTimeOffset? a11, global::System.String? a12, global::System.Byte[]? a13, global::System.Int32[]? a14, global::System.Double[]? a15, global::System.String[]? a16);""");
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
        Contains("""Proxies.Set("Space.Class.FunA", (global::Space.Record a) => Deserialize<global::Space.Record>(Space_Class_FunA(Serialize(a))));""");
        Contains("""Proxies.Set("Space.Class.FunB", async (global::Space.Record?[]? a) => Deserialize<global::Space.Record?[]?>(await Space_Class_FunB(Serialize(a))));""");
        Contains("JSExport] internal static global::System.String Space_Class_InvA (global::System.String a) => Serialize(global::Space.Class.InvA(Deserialize<global::Space.Record>(a)));");
        Contains("JSExport] internal static async global::System.Threading.Tasks.Task<global::System.String?> Space_Class_InvB (global::System.String? a) => Serialize(await global::Space.Class.InvB(Deserialize<global::Space.Record?[]?>(a)));");
        Contains("""JSImport("Space.Class.funASerialized", "Bootsharp")] internal static partial global::System.String Space_Class_FunA (global::System.String a);""");
        Contains("""JSImport("Space.Class.funBSerialized", "Bootsharp")] internal static partial global::System.Threading.Tasks.Task<global::System.String?> Space_Class_FunB (global::System.String? a);""");

        // TODO: Remove when resolved: https://github.com/elringus/bootsharp/issues/138
        Contains("""Proxies.Set("Space.Class.FunAsyncBytes", async () => Deserialize<global::System.Byte[]>(await Space_Class_FunAsyncBytes()));""");
        Contains("JSExport] internal static async global::System.Threading.Tasks.Task<global::System.String> Space_Class_InvAsyncBytes () => Serialize(await global::Space.Class.InvAsyncBytes());");
        Contains("""JSImport("Space.Class.funAsyncBytesSerialized", "Bootsharp")] internal static partial global::System.Threading.Tasks.Task<global::System.String> Space_Class_FunAsyncBytes ();""");
    }

    [Fact]
    public void RespectsSpacePreference ()
    {
        AddAssembly(With(
            """
            [assembly:JSPreferences(Space = [@"Space", "Foo"])]
            [assembly:JSExport(typeof(Space.IExported))]
            [assembly:JSImport(typeof(Space.IImported))]

            namespace Space;

            public interface IExported { void Inv (); }
            public interface IImported { void Fun (); void NotifyEvt(); }

            public class Class
            {
                [JSInvokable] public static void Inv () {}
                [JSFunction] public static void Fun () => Proxies.Get<Action>("Class.Fun")();
                [JSEvent] public static void Evt () => Proxies.Get<Action>("Class.Evt")();
            }
            """));
        Execute();
        Contains("""Proxies.Set("Bootsharp.Generated.Imports.Space.JSImported.Fun", () => Bootsharp_Generated_Imports_Space_JSImported_Fun());""");
        Contains("""Proxies.Set("Bootsharp.Generated.Imports.Space.JSImported.OnEvt", () => Bootsharp_Generated_Imports_Space_JSImported_OnEvt());""");
        Contains("""Proxies.Set("Space.Class.Fun", () => Space_Class_Fun());""");
        Contains("""Proxies.Set("Space.Class.Evt", () => Space_Class_Evt());""");
        Contains("JSExport] internal static void Space_Class_Inv () => global::Space.Class.Inv();");
        Contains("""JSImport("Foo.Class.funSerialized", "Bootsharp")] internal static partial void Space_Class_Fun ();""");
        Contains("""JSImport("Foo.Class.evtSerialized", "Bootsharp")] internal static partial void Space_Class_Evt ();""");
        Contains("JSExport] internal static void Bootsharp_Generated_Exports_Space_JSExported_Inv () => global::Bootsharp.Generated.Exports.Space.JSExported.Inv();");
        Contains("""JSImport("Foo.Imported.funSerialized", "Bootsharp")] internal static partial void Bootsharp_Generated_Imports_Space_JSImported_Fun ();""");
        Contains("""JSImport("Foo.Imported.onEvtSerialized", "Bootsharp")] internal static partial void Bootsharp_Generated_Imports_Space_JSImported_OnEvt ();""");
    }
}
