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
            namespace Bootsharp.Generated;

            public static partial class Interop
            {
            """);
    }

    [Fact]
    public void GeneratesDisposeInstanceBindings ()
    {
        Execute();
        Contains("JSExport] internal static void DisposeExportedInstance (int id) => Instances.Dispose(id);");
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
                [JSInvokable] public static Task<Space.IExported> GetExported (Space.IImported arg) => default;
                [JSFunction] public static Task<IImported> GetImported (IExported arg) => Proxies.Get<Func<IExported, Task<IImported>>>("Class.GetImported")(arg);
            }
            """));
        Execute();
        Contains("JSExport] internal static async global::System.Threading.Tasks.Task<global::System.Int32> Class_GetExported (global::System.Int32 arg) => Instances.Register(await global::Class.GetExported(new global::Bootsharp.Generated.Imports.Space.JSImported(arg)));");
        Contains("""JSImport("Class.getImportedSerialized", "Bootsharp")] internal static partial global::System.Threading.Tasks.Task<global::System.Int32> Class_GetImported (global::System.Int32 arg);""");
        Contains("JSExport] internal static void Bootsharp_Generated_Exports_JSExported_Inv (global::System.Int32 _id) => ((global::IExported)Instances.Get(_id)).Inv();");
        Contains("""JSImport("Imported.onEvtSerialized", "Bootsharp")] internal static partial void Bootsharp_Generated_Imports_JSImported_OnEvt (global::System.Int32 _id);""");
        Contains("JSExport] internal static void Bootsharp_Generated_Exports_Space_JSExported_Inv (global::System.Int32 _id) => ((global::Space.IExported)Instances.Get(_id)).Inv();");
        Contains("""JSImport("Space.Imported.funSerialized", "Bootsharp")] internal static partial void Bootsharp_Generated_Imports_Space_JSImported_Fun (global::System.Int32 _id);""");
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
        DoesNotContain("Foo");
    }

    [Fact]
    public void DoesntSerializeTypesThatShouldNotBeSerialized ()
    {
        AddAssembly(With(
            """
            namespace Space;

            public class Class
            {
                [JSInvokable] public static Task<Exception> Inv (bool a1, byte a2, char a3, short a4, long a5, int a6, float a7, double a8, nint a9, DateTime a10, DateTimeOffset a11, string a12) => default!;
                [JSInvokable] public static Task<DateTime> InvNull (bool? a1, byte? a2, char? a3, short? a4, long? a5, int? a6, float? a7, double? a8, nint? a9, DateTime? a10, DateTimeOffset? a11, string? a12) => default!;
                [JSFunction] public static Task<Exception> Fun (bool a1, byte a2, char a3, short a4, long a5, int a6, float a7, double a8, nint a9, DateTime a10, DateTimeOffset a11, string a12) => Proxies.Get<Func<bool, byte, char, short, long, int, float, double, nint, DateTime, DateTimeOffset, string, Task<Exception>>>("Space.Class.Fun")(a1, a2, a3, a4, a5, a6, a7, a8, a9, a10, a11, a12);
                [JSFunction] public static Task<DateTime> FunNull (bool? a1, byte? a2, char? a3, short? a4, long? a5, int? a6, float? a7, double? a8, nint? a9, DateTime? a10, DateTimeOffset? a11, string? a12) => Proxies.Get<Func<bool?, byte?, char?, short?, long?, int?, float?, double?, nint?, DateTime?, DateTimeOffset?, string?, Task<DateTime>>>("Space.Class.FunNull")(a1, a2, a3, a4, a5, a6, a7, a8, a9, a10, a11, a12);
            }
            """));
        Execute();
        Contains("JSExport] internal static global::System.Threading.Tasks.Task<global::System.Exception> Space_Class_Inv (global::System.Boolean a1, global::System.Byte a2, global::System.Char a3, global::System.Int16 a4, [JSMarshalAs<JSType.BigInt>] global::System.Int64 a5, global::System.Int32 a6, global::System.Single a7, global::System.Double a8, global::System.IntPtr a9, [JSMarshalAs<JSType.Date>] global::System.DateTime a10, [JSMarshalAs<JSType.Date>] global::System.DateTimeOffset a11, global::System.String a12) => global::Space.Class.Inv(a1, a2, a3, a4, a5, a6, a7, a8, a9, a10, a11, a12);");
        Contains("JSExport] [return: JSMarshalAs<JSType.Promise<JSType.Date>>] internal static global::System.Threading.Tasks.Task<global::System.DateTime> Space_Class_InvNull (global::System.Boolean? a1, global::System.Byte? a2, global::System.Char? a3, global::System.Int16? a4, [JSMarshalAs<JSType.BigInt>] global::System.Int64? a5, global::System.Int32? a6, global::System.Single? a7, global::System.Double? a8, global::System.IntPtr? a9, [JSMarshalAs<JSType.Date>] global::System.DateTime? a10, [JSMarshalAs<JSType.Date>] global::System.DateTimeOffset? a11, global::System.String? a12) => global::Space.Class.InvNull(a1, a2, a3, a4, a5, a6, a7, a8, a9, a10, a11, a12);");
        Contains("""JSImport("Space.Class.funSerialized", "Bootsharp")] internal static partial global::System.Threading.Tasks.Task<global::System.Exception> Space_Class_Fun (global::System.Boolean a1, global::System.Byte a2, global::System.Char a3, global::System.Int16 a4, [JSMarshalAs<JSType.BigInt>] global::System.Int64 a5, global::System.Int32 a6, global::System.Single a7, global::System.Double a8, global::System.IntPtr a9, [JSMarshalAs<JSType.Date>] global::System.DateTime a10, [JSMarshalAs<JSType.Date>] global::System.DateTimeOffset a11, global::System.String a12);""");
        Contains("public static global::System.Threading.Tasks.Task<global::System.DateTime> Proxy_Space_Class_FunNull(global::System.Boolean? a1, global::System.Byte? a2, global::System.Char? a3, global::System.Int16? a4, global::System.Int64? a5, global::System.Int32? a6, global::System.Single? a7, global::System.Double? a8, global::System.IntPtr? a9, global::System.DateTime? a10, global::System.DateTimeOffset? a11, global::System.String? a12) => Space_Class_FunNull(a1, a2, a3, a4, a5, a6, a7, a8, a9, a10, a11, a12);");
        Contains("""JSImport("Space.Class.funNullSerialized", "Bootsharp")] [return: JSMarshalAs<JSType.Promise<JSType.Date>>] internal static partial global::System.Threading.Tasks.Task<global::System.DateTime> Space_Class_FunNull (global::System.Boolean? a1, global::System.Byte? a2, global::System.Char? a3, global::System.Int16? a4, [JSMarshalAs<JSType.BigInt>] global::System.Int64? a5, global::System.Int32? a6, global::System.Single? a7, global::System.Double? a8, global::System.IntPtr? a9, [JSMarshalAs<JSType.Date>] global::System.DateTime? a10, [JSMarshalAs<JSType.Date>] global::System.DateTimeOffset? a11, global::System.String? a12);""");
        Contains("public static global::System.Threading.Tasks.Task<global::System.DateTime> Proxy_Space_Class_FunNull(global::System.Boolean? a1, global::System.Byte? a2, global::System.Char? a3, global::System.Int16? a4, global::System.Int64? a5, global::System.Int32? a6, global::System.Single? a7, global::System.Double? a8, global::System.IntPtr? a9, global::System.DateTime? a10, global::System.DateTimeOffset? a11, global::System.String? a12) => Space_Class_FunNull(a1, a2, a3, a4, a5, a6, a7, a8, a9, a10, a11, a12);");
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
            }
            """));
        Execute();
        Contains("JSExport] [return: JSMarshalAs<JSType.BigInt>] internal static global::System.Int64 Space_Class_InvA ([JSMarshalAs<JSType.BigInt>] global::System.Int64 a) => Serializer.Serialize(global::Space.Class.InvA(Serializer.Deserialize(a, SerializerContext.Space_Record)), SerializerContext.Space_Record);");
        Contains("JSExport] [return: JSMarshalAs<JSType.Promise<JSType.BigInt>>] internal static async global::System.Threading.Tasks.Task<global::System.Int64> Space_Class_InvB ([JSMarshalAs<JSType.BigInt>] global::System.Int64 a) => Serializer.Serialize(await global::Space.Class.InvB(Serializer.Deserialize(a, SerializerContext.Space_RecordArray)), SerializerContext.Space_RecordArray);");
        Contains("""JSImport("Space.Class.funASerialized", "Bootsharp")] [return: JSMarshalAs<JSType.BigInt>] internal static partial global::System.Int64 Space_Class_FunA ([JSMarshalAs<JSType.BigInt>] global::System.Int64 a);""");
        Contains("public static global::Space.Record Proxy_Space_Class_FunA(global::Space.Record a) => Serializer.Deserialize(Space_Class_FunA(Serializer.Serialize(a, SerializerContext.Space_Record)), SerializerContext.Space_Record);");
        Contains("""JSImport("Space.Class.funBSerialized", "Bootsharp")] [return: JSMarshalAs<JSType.Promise<JSType.BigInt>>] internal static partial global::System.Threading.Tasks.Task<global::System.Int64> Space_Class_FunB ([JSMarshalAs<JSType.BigInt>] global::System.Int64 a);""");
        Contains("public static async global::System.Threading.Tasks.Task<global::Space.Record?[]?> Proxy_Space_Class_FunB(global::Space.Record?[]? a) => Serializer.Deserialize(await Space_Class_FunB(Serializer.Serialize(a, SerializerContext.Space_RecordArray)), SerializerContext.Space_RecordArray);");
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
        Contains("JSExport] internal static void Space_Class_Inv () => global::Space.Class.Inv();");
        Contains("""JSImport("Foo.Class.funSerialized", "Bootsharp")] internal static partial void Space_Class_Fun ();""");
        Contains("""JSImport("Foo.Class.evtSerialized", "Bootsharp")] internal static partial void Space_Class_Evt ();""");
        Contains("JSExport] internal static void Bootsharp_Generated_Exports_Space_JSExported_Inv () => global::Bootsharp.Generated.Exports.Space.JSExported.Inv();");
        Contains("""JSImport("Foo.Imported.funSerialized", "Bootsharp")] internal static partial void Bootsharp_Generated_Imports_Space_JSImported_Fun ();""");
        Contains("""JSImport("Foo.Imported.onEvtSerialized", "Bootsharp")] internal static partial void Bootsharp_Generated_Imports_Space_JSImported_OnEvt ();""");
    }
}
