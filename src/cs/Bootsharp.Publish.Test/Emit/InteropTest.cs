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
            using System.Linq;

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
        Contains("JSExport] internal static void DisposeExportedInstance (int id) => global::Bootsharp.Instances.Dispose(id);");
        Contains("""JSImport("disposeInstance", "Bootsharp")] internal static partial void DisposeImportedInstance (int id);""");
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
        Contains("""JSImport("Class.funMarshalled", "Bootsharp")] internal static partial void Class_Fun ();""");
        Contains("""JSImport("Class.evtMarshalled", "Bootsharp")] internal static partial void Class_Evt ();""");
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
        Contains("""JSImport("SpaceA.Class.funMarshalled", "Bootsharp")] internal static partial void SpaceA_Class_Fun ();""");
        Contains("""JSImport("SpaceA.Class.evtMarshalled", "Bootsharp")] internal static partial void SpaceA_Class_Evt ();""");
        Contains("JSExport] internal static void SpaceA_SpaceB_Class_Inv () => global::SpaceA.SpaceB.Class.Inv();");
        Contains("""JSImport("SpaceA.SpaceB.Class.funMarshalled", "Bootsharp")] internal static partial void SpaceA_SpaceB_Class_Fun ();""");
        Contains("""JSImport("SpaceA.SpaceB.Class.evtMarshalled", "Bootsharp")] internal static partial void SpaceA_SpaceB_Class_Evt ();""");
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
        Contains("""JSImport("Imported.funMarshalled", "Bootsharp")] internal static partial void Bootsharp_Generated_Imports_JSImported_Fun ();""");
        Contains("""JSImport("Imported.onEvtMarshalled", "Bootsharp")] internal static partial void Bootsharp_Generated_Imports_JSImported_OnEvt ();""");
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
                [JSInvokable] public static Task<Space.IExported> GetExported (Space.IImported arg) => default;
                [JSFunction] public static Task<IImported> GetImported (IExported arg) => Proxies.Get<Func<IExported, Task<IImported>>>("Class.GetImported")(arg);
            }
            """));
        Execute();
        Contains("""Proxies.Set("Class.GetImported", async (global::IExported arg) => (global::IImported)new global::Bootsharp.Generated.Imports.JSImported(await Class_GetImported(global::Bootsharp.Instances.Register(arg))));""");
        Contains("""Proxies.Set("Bootsharp.Generated.Imports.JSImported.OnEvt", (global::System.Int32 _id) => Bootsharp_Generated_Imports_JSImported_OnEvt(_id));""");
        Contains("""Proxies.Set("Bootsharp.Generated.Imports.Space.JSImported.Fun", (global::System.Int32 _id) => Bootsharp_Generated_Imports_Space_JSImported_Fun(_id));""");
        Contains("JSExport] internal static async global::System.Threading.Tasks.Task<global::System.Int32> Class_GetExported (global::System.Int32 arg) => global::Bootsharp.Instances.Register(await global::Class.GetExported(new global::Bootsharp.Generated.Imports.Space.JSImported(arg)));");
        Contains("""JSImport("Class.getImportedMarshalled", "Bootsharp")] internal static partial global::System.Threading.Tasks.Task<global::System.Int32> Class_GetImported (global::System.Int32 arg);""");
        Contains("JSExport] internal static void Bootsharp_Generated_Exports_JSExported_Inv (global::System.Int32 _id) => ((global::IExported)global::Bootsharp.Instances.Get(_id)).Inv();");
        Contains("""JSImport("Imported.onEvtMarshalled", "Bootsharp")] internal static partial void Bootsharp_Generated_Imports_JSImported_OnEvt (global::System.Int32 _id);""");
        Contains("JSExport] internal static void Bootsharp_Generated_Exports_Space_JSExported_Inv (global::System.Int32 _id) => ((global::Space.IExported)global::Bootsharp.Instances.Get(_id)).Inv();");
        Contains("""JSImport("Space.Imported.funMarshalled", "Bootsharp")] internal static partial void Bootsharp_Generated_Imports_Space_JSImported_Fun (global::System.Int32 _id);""");
    }

    [Fact]
    public void IgnoresImplementedInterfaceMethods ()
    {
        AddAssembly(With(
            """
            [assembly:JSExport(typeof(IExportedStatic))]
            [assembly:JSImport(typeof(IImportedStatic))]

            public interface IExportedStatic { int Foo () => 0; }
            public interface IImportedStatic { int Foo () => 0; }
            public interface IExportedInstanced { int Foo () => 0; }
            public interface IImportedInstanced { int Foo () => 0; }

            public class Class
            {
                [JSInvokable] public static IExportedInstanced GetExported () => default;
                [JSFunction] public static IImportedInstanced GetImported () => default;
            }
            """));
        Execute();
        Assert.DoesNotContain("Foo", TestedContent, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void DoesntMarshalTypesThatShouldNotBeMarshalled ()
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
        Contains("""JSImport("Space.Class.funMarshalled", "Bootsharp")] internal static partial global::System.Threading.Tasks.Task<global::System.Exception> Space_Class_Fun (global::System.Boolean a1, global::System.Byte a2, global::System.Char a3, global::System.Int16 a4, [JSMarshalAs<JSType.BigInt>] global::System.Int64 a5, global::System.Int32 a6, global::System.Single a7, global::System.Double a8, global::System.IntPtr a9, [JSMarshalAs<JSType.Date>] global::System.DateTime a10, [JSMarshalAs<JSType.Date>] global::System.DateTimeOffset a11, global::System.String a12, global::System.Byte[] a13, global::System.Int32[] a14, global::System.Double[] a15, global::System.String[] a16);""");
        Contains("""JSImport("Space.Class.funNullMarshalled", "Bootsharp")] [return: JSMarshalAs<JSType.Promise<JSType.Date>>] internal static partial global::System.Threading.Tasks.Task<global::System.DateTime> Space_Class_FunNull (global::System.Boolean? a1, global::System.Byte? a2, global::System.Char? a3, global::System.Int16? a4, [JSMarshalAs<JSType.BigInt>] global::System.Int64? a5, global::System.Int32? a6, global::System.Single? a7, global::System.Double? a8, global::System.IntPtr? a9, [JSMarshalAs<JSType.Date>] global::System.DateTime? a10, [JSMarshalAs<JSType.Date>] global::System.DateTimeOffset? a11, global::System.String? a12, global::System.Byte[]? a13, global::System.Int32[]? a14, global::System.Double[]? a15, global::System.String[]? a16);""");
    }

    [Fact]
    public void MarshalsTypesThatShouldBeMarshalled ()
    {
        AddAssembly(With(
            """
            namespace Space;

            public record Record;

            public class Class
            {
                [JSInvokable] public static Record InvA (Record a) => default;
                [JSInvokable] public static Task<Record?[]?> InvB (Record?[]? a) => default;
                [JSInvokable] public static Task<byte[]> InvC () => default;
                [JSFunction] public static Record FunA (Record a) => Proxies.Get<Func<Record, Record>>("Space.Class.FunA")(a);
                [JSFunction] public static Task<Record?[]?> FunB (Record?[]? a) => Proxies.Get<Func<Record?[]?, Task<Record?[]?>>>("Space.Class.FunB")(a);
                [JSFunction] public static Task<byte[]> FunC () => Proxies.Get<Func<Task<byte[]>>>("Space.Class.FunC")();
            }
            """));
        Execute();
        Contains("""Proxies.Set("Space.Class.FunA", (global::Space.Record a) => Unmarshal_Space_Record(Space_Class_FunA(Marshal_Space_Record(a))));""");
        Contains("""Proxies.Set("Space.Class.FunB", async (global::Space.Record?[]? a) => Unmarshal_Space_Record_Array(await Space_Class_FunB(Marshal_Space_Record_Array(a))));""");
        Contains("""Proxies.Set("Space.Class.FunC", async () => (await Space_Class_FunC()));""");
        Contains("JSExport] [return: JSMarshalAs<JSType.Any>] internal static global::System.Object Space_Class_InvA ([JSMarshalAs<JSType.Any>] global::System.Object a) => Marshal_Space_Record(global::Space.Class.InvA(Unmarshal_Space_Record(a)));");
        Contains("JSExport] [return: JSMarshalAs<JSType.Promise<JSType.Any>>] internal static async global::System.Threading.Tasks.Task<global::System.Object?> Space_Class_InvB ([JSMarshalAs<JSType.Any>] global::System.Object? a) => Marshal_Space_Record_Array(await global::Space.Class.InvB(Unmarshal_Space_Record_Array(a)));");
        Contains("JSExport] [return: JSMarshalAs<JSType.Promise<JSType.Any>>] internal static async global::System.Threading.Tasks.Task<global::System.Object> Space_Class_InvC () => (await global::Space.Class.InvC());");
        Contains("""JSImport("Space.Class.funAMarshalled", "Bootsharp")] [return: JSMarshalAs<JSType.Any>] internal static partial global::System.Object Space_Class_FunA ([JSMarshalAs<JSType.Any>] global::System.Object a);""");
        Contains("""JSImport("Space.Class.funBMarshalled", "Bootsharp")] [return: JSMarshalAs<JSType.Promise<JSType.Any>>] internal static partial global::System.Threading.Tasks.Task<global::System.Object?> Space_Class_FunB ([JSMarshalAs<JSType.Any>] global::System.Object? a);""");
        Contains("""JSImport("Space.Class.funCMarshalled", "Bootsharp")] [return: JSMarshalAs<JSType.Promise<JSType.Any>>] internal static partial global::System.Threading.Tasks.Task<global::System.Object> Space_Class_FunC ();""");
    }

    [Fact]
    public void GeneratesMarshalMethods ()
    {
        AddAssembly(With(
            """
            namespace Space;

            public struct Struct { public int Int { get; set; } }
            public record Record (string String, int Int, int? NullInt, byte[] ByteArr, Struct Struct,
                IReadOnlyList<byte> ByteList, IList<Struct> StructList, IReadOnlyDictionary<int, string> Dict,
                Dictionary<Struct, Struct> StructDict);

            public class Class
            {
                [JSInvokable] public static Record Inv () => default;
            }
            """));
        Execute();
        Contains("private static object Marshal_Space_Struct (global::Space.Struct obj) => new object[] { obj.Int };");
        Contains("private static object Marshal_Space_Record (global::Space.Record obj) => obj is null ? null : new object[] { obj.String is null ? null : obj.String, obj.Int, obj.NullInt is null ? null : obj.NullInt, obj.ByteArr is null ? null : obj.ByteArr, Marshal_Space_Struct(obj.Struct), obj.ByteList is null ? null : obj.ByteList.ToArray(), obj.StructList is null ? null : obj.StructList.Select(Marshal_Space_Struct).ToArray(), obj.Dict is null ? null : (object[])[..obj.Dict.Keys, ..obj.Dict.Values], obj.StructDict is null ? null : (object[])[..obj.StructDict.Keys.Select(Marshal_Space_Struct), ..obj.StructDict.Values.Select(Marshal_Space_Struct)] };");
    }

    [Fact]
    public void RespectsSpacePreference ()
    {
        AddAssembly(With(
            """
            [assembly:JSPreferences(Space = ["Space", "Foo"])]
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
        Contains("""JSImport("Foo.Class.funMarshalled", "Bootsharp")] internal static partial void Space_Class_Fun ();""");
        Contains("""JSImport("Foo.Class.evtMarshalled", "Bootsharp")] internal static partial void Space_Class_Evt ();""");
        Contains("JSExport] internal static void Bootsharp_Generated_Exports_Space_JSExported_Inv () => global::Bootsharp.Generated.Exports.Space.JSExported.Inv();");
        Contains("""JSImport("Foo.Imported.funMarshalled", "Bootsharp")] internal static partial void Bootsharp_Generated_Imports_Space_JSImported_Fun ();""");
        Contains("""JSImport("Foo.Imported.onEvtMarshalled", "Bootsharp")] internal static partial void Bootsharp_Generated_Imports_Space_JSImported_OnEvt ();""");
    }
}
