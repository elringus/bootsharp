namespace Bootsharp.Publish.Test;

public class InteropTest : GenerateCSTest
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
    public void GeneratesInitializersForEntryAndLibraryAssemblies ()
    {
        AddAssembly("Library.dll",
            With("Library",
                """
                public static class Registry
                {
                    [Export] public static event Action<string>? Evt;
                    // (the fields are emitted by the source generators)
                    public static unsafe delegate* managed<int, string> Bootsharp_GetLabel;
                    [Import] public static string GetLabel (int count) => default!;
                    public static unsafe delegate* managed<int> Bootsharp_GetCount;
                    public static unsafe delegate* managed<int, void> Bootsharp_SetCount;
                    [Import] public static int Count { get => default!; set { } }
                }
                """));
        AddAssembly("Entry.dll",
            With("Entry",
                """
                public static class App
                {
                    // (the field is emitted by the source generators)
                    public static unsafe delegate* managed<string> Bootsharp_GetName;
                    [Import] public static string GetName () => default!;
                }
                """));
        Execute();
        Contains(
            """
                [ModuleInitializer]
                internal static unsafe void Initialize ()
                {
                    global::Entry.App.Bootsharp_GetName = &Entry_App_GetName;
                    global::Library.Registry.Evt += Handle_Library_Registry_Evt;
                    global::Library.Registry.Bootsharp_GetLabel = &Library_Registry_GetLabel;
                    global::Library.Registry.Bootsharp_GetCount = &Library_Registry_GetCount;
                    global::Library.Registry.Bootsharp_SetCount = &Library_Registry_SetCount;
                }
            """);
    }

    [Fact]
    public void GeneratesForMembersWithoutNamespace ()
    {
        AddAssembly(With(
            """
            public partial class Class
            {
                [Export] public static event Action? ExpEvt;
                [Import] public static event Action? ImpEvt;
                [Export] public static void Inv () {}
                [Import] public static void Fun () => Proxies.Get<Action>("Class.Fun")();
                [Export] public static int ExpProp { get; set; }
                [Import] public static int ImpProp { get => default; set { } }
            }
            """));
        Execute();
        Contains("void Handle_Class_ExpEvt () => Class_BroadcastExpEvt_Serialized();");
        Contains("""[JSImport("Class.broadcastExpEvtSerialized", "index")] internal static partial void Class_BroadcastExpEvt_Serialized ();""");
        Contains("[JSExport] internal static void Class_InvokeImpEvt () => global::Class.Bootsharp_Invoke_ImpEvt();");
        Contains("[JSExport] internal static void Class_Inv () => global::Class.Inv();");
        Contains("""[JSImport("Class.funSerialized", "index")] internal static partial void Class_Fun_Serialized ();""");
        Contains("[JSExport] internal static global::System.Int32 Class_GetExpProp () => global::Class.ExpProp;");
        Contains("[JSExport] internal static void Class_SetExpProp (global::System.Int32 value) => global::Class.ExpProp = value;");
        Contains("""[JSImport("Class.getImpPropSerialized", "index")] internal static partial global::System.Int32 Class_GetImpProp_Serialized ();""");
        Contains("public static global::System.Int32 Class_GetImpProp() => Class_GetImpProp_Serialized();");
        Contains("""[JSImport("Class.setImpPropSerialized", "index")] internal static partial void Class_SetImpProp_Serialized (global::System.Int32 value);""");
        Contains("public static void Class_SetImpProp(global::System.Int32 value) => Class_SetImpProp_Serialized(value);");
    }

    [Fact]
    public void GeneratesForMembersInCustomSpaces ()
    {
        AddAssembly(With(
            """
            namespace SpaceA
            {
                public partial class Class
                {
                    [Export] public static event Action? ExpEvt;
                    [Export] public static void Inv () {}
                    [Import] public static void Fun () => Proxies.Get<Action>("SpaceA.Class.Fun")();
                    [Export] public static int ExpProp { get; set; }
                }
            }
            namespace SpaceA.SpaceB
            {
                public partial class Class
                {
                    [Import] public static event Action? ImpEvt;
                    [Export] public static void Inv () {}
                    [Import] public static void Fun () => Proxies.Get<Action>("SpaceA.SpaceB.Class.Fun")();
                    [Import] public static int ImpProp { get => default; set { } }
                }
            }
            """));
        Execute();
        Contains("""[JSImport("Class.broadcastExpEvtSerialized", "space-a")] internal static partial void SpaceA_Class_BroadcastExpEvt_Serialized ();""");
        Contains("void Handle_SpaceA_Class_ExpEvt () => SpaceA_Class_BroadcastExpEvt_Serialized();");
        Contains("[JSExport] internal static void SpaceA_Class_Inv () => global::SpaceA.Class.Inv();");
        Contains("""[JSImport("Class.funSerialized", "space-a")] internal static partial void SpaceA_Class_Fun_Serialized ();""");
        Contains("[JSExport] internal static void SpaceA_SpaceB_Class_InvokeImpEvt () => global::SpaceA.SpaceB.Class.Bootsharp_Invoke_ImpEvt();");
        Contains("[JSExport] internal static void SpaceA_SpaceB_Class_Inv () => global::SpaceA.SpaceB.Class.Inv();");
        Contains("""[JSImport("Class.funSerialized", "space-a/space-b")] internal static partial void SpaceA_SpaceB_Class_Fun_Serialized ();""");
        Contains("[JSExport] internal static global::System.Int32 SpaceA_Class_GetExpProp () => global::SpaceA.Class.ExpProp;");
        Contains("[JSExport] internal static void SpaceA_Class_SetExpProp (global::System.Int32 value) => global::SpaceA.Class.ExpProp = value;");
        Contains("""[JSImport("Class.getImpPropSerialized", "space-a/space-b")] internal static partial global::System.Int32 SpaceA_SpaceB_Class_GetImpProp_Serialized ();""");
        Contains("public static global::System.Int32 SpaceA_SpaceB_Class_GetImpProp() => SpaceA_SpaceB_Class_GetImpProp_Serialized();");
        Contains("""[JSImport("Class.setImpPropSerialized", "space-a/space-b")] internal static partial void SpaceA_SpaceB_Class_SetImpProp_Serialized (global::System.Int32 value);""");
        Contains("public static void SpaceA_SpaceB_Class_SetImpProp(global::System.Int32 value) => SpaceA_SpaceB_Class_SetImpProp_Serialized(value);");
    }

    [Fact]
    public void IgnoresEventsWithoutImportExportAttributes ()
    {
        AddAssembly(WithClass("public static event Action? Evt;"));
        Execute();
        DoesNotContain("Evt");
    }

    [Fact]
    public void GeneratesForMethodsInModules ()
    {
        AddAssembly(With(
            """
            [assembly:Export(typeof(Space.IExported))]
            [assembly:Import(typeof(Space.IImported))]

            namespace Space;

            public record Info (string Value);

            public interface IExported { Info Inv (string str, Info info); }
            public interface IImported { Info Fun (string str, Info info); }
            """));
        Execute();
        Contains("[JSExport] [return: JSMarshalAs<JSType.BigInt>] internal static long Bootsharp_Generated_Exports_Space_JSExported_Inv (global::System.String str, [JSMarshalAs<JSType.BigInt>] long info) => Serializer.Serialize(global::Bootsharp.Generated.Exports.Space.JSExported.Inv(str, Serializer.Deserialize(info, SerializerContext.Space_Info)), SerializerContext.Space_Info);");
        Contains("""[JSImport("IImported.funSerialized", "space")] [return: JSMarshalAs<JSType.BigInt>] internal static partial long Bootsharp_Generated_Imports_Space_JSImported_Fun_Serialized (global::System.String str, [JSMarshalAs<JSType.BigInt>] long info);""");
        Contains("public static global::Space.Info Bootsharp_Generated_Imports_Space_JSImported_Fun (global::System.String str, global::Space.Info info) => Serializer.Deserialize(Bootsharp_Generated_Imports_Space_JSImported_Fun_Serialized(str, Serializer.Serialize(info, SerializerContext.Space_Info)), SerializerContext.Space_Info);");
    }

    [Fact]
    public void GeneratesForMethodsInInstanced ()
    {
        AddAssembly(With(
            """
            public record Info (string Value);

            public interface IExported { Info Inv (IExported it, Info info); }
            public interface IImported { Info Fun (IImported it, Info info); }

            public partial class Class
            {
                [Export] public static Task<IExported> GetExported (IImported it) => default;
                [Import] public static Task<IImported> GetImported (IExported it) => default;
            }
            """));
        Execute();
        Contains("[JSExport] [return: JSMarshalAs<JSType.BigInt>] internal static long Bootsharp_Generated_Exports_JSExported_Inv (int _id, int it, [JSMarshalAs<JSType.BigInt>] long info) => Serializer.Serialize(Instances.Exported<global::IExported>(_id).Inv(Instances.Resolve<global::IExported>(it), Serializer.Deserialize(info, SerializerContext.Info)), SerializerContext.Info);");
        Contains("""[JSImport("IImported.funSerialized", "index")] [return: JSMarshalAs<JSType.BigInt>] internal static partial long Bootsharp_Generated_Imports_JSImported_Fun_Serialized (int _id, int it, [JSMarshalAs<JSType.BigInt>] long info);""");
        Contains("public static global::Info Bootsharp_Generated_Imports_JSImported_Fun (int _id, global::IImported it, global::Info info) => Serializer.Deserialize(Bootsharp_Generated_Imports_JSImported_Fun_Serialized(_id, Instances.Export(it), Serializer.Serialize(info, SerializerContext.Info)), SerializerContext.Info);");
        Contains("[JSExport] internal static void Class_GetExported (int _taskId, int it)");
        Contains("var _result = Instances.Export(await global::Class.GetExported(Instances.Resolve<global::IImported>(it)));");
        Contains("Class_GetExported_Notify(_taskId, _result);");
        Contains("Class_GetExported_Fail(_taskId, Serializer.Serialize(_e.Message, Serializer.String));");
        Contains("""[JSImport("Class.getExportedNotify", "index")] internal static partial void Class_GetExported_Notify (int _taskId, int _result);""");
        Contains("""[JSImport("Class.getExportedFail", "index")] internal static partial void Class_GetExported_Fail (int _taskId, [JSMarshalAs<JSType.BigInt>] long _message);""");
        Contains("""[JSImport("Class.getImportedSerialized", "index")] internal static partial void Class_GetImported_Serialized (int _taskId, int it);""");
        Contains("[JSExport] internal static void Class_GetImported_Complete (int _taskId, int _result) => PendingImports.Take<global::IImported>(_taskId).SetResult(Instances.Resolve<global::IImported>(_result));");
        Contains("[JSExport] internal static void Class_GetImported_Fail (int _taskId, [JSMarshalAs<JSType.BigInt>] long _message) => PendingImports.Take<global::IImported>(_taskId).SetException(new JSException(Serializer.Deserialize(_message, Serializer.String)!));");
        Contains("public static global::System.Threading.Tasks.Task<global::IImported> Class_GetImported (global::IExported it)");
        Contains("var _tcs = new global::System.Threading.Tasks.TaskCompletionSource<global::IImported>();");
        Contains("Class_GetImported_Serialized(_taskId, Instances.Export(it));");
    }

    [Fact]
    public void GeneratesForPropertiesInModules ()
    {
        AddAssembly(With(
            """
            [assembly:Export(typeof(Space.IExported))]
            [assembly:Import(typeof(Space.IImported))]

            namespace Space;

            public record Info;

            public interface IExported
            {
                Info State { get; set; }
                bool Active { get; }
                int Count { set; }
            }

            public interface IImported
            {
                Info State { get; set; }
                bool Active { get; }
                int Count { set; }
            }
            """));
        Execute();
        Contains("[JSExport] [return: JSMarshalAs<JSType.BigInt>] internal static long Bootsharp_Generated_Exports_Space_JSExported_GetState () => Serializer.Serialize(global::Bootsharp.Generated.Exports.Space.JSExported.GetState(), SerializerContext.Space_Info);");
        Contains("[JSExport] internal static void Bootsharp_Generated_Exports_Space_JSExported_SetState ([JSMarshalAs<JSType.BigInt>] long value) => global::Bootsharp.Generated.Exports.Space.JSExported.SetState(Serializer.Deserialize(value, SerializerContext.Space_Info));");
        Contains("""[JSImport("IImported.getStateSerialized", "space")] [return: JSMarshalAs<JSType.BigInt>] internal static partial long Space_IImported_GetState_Serialized ();""");
        Contains("public static global::Space.Info Bootsharp_Generated_Imports_Space_JSImported_GetState() => Serializer.Deserialize(Space_IImported_GetState_Serialized(), SerializerContext.Space_Info);");
        Contains("""[JSImport("IImported.setStateSerialized", "space")] internal static partial void Space_IImported_SetState_Serialized ([JSMarshalAs<JSType.BigInt>] long value);""");
        Contains("public static void Bootsharp_Generated_Imports_Space_JSImported_SetState(global::Space.Info value) => Space_IImported_SetState_Serialized(Serializer.Serialize(value, SerializerContext.Space_Info));");
        Contains("[JSExport] internal static global::System.Boolean Bootsharp_Generated_Exports_Space_JSExported_GetActive () => global::Bootsharp.Generated.Exports.Space.JSExported.GetActive();");
        Contains("""[JSImport("IImported.getActiveSerialized", "space")] internal static partial global::System.Boolean Space_IImported_GetActive_Serialized ();""");
        Contains("public static global::System.Boolean Bootsharp_Generated_Imports_Space_JSImported_GetActive() => Space_IImported_GetActive_Serialized();");
        Contains("[JSExport] internal static void Bootsharp_Generated_Exports_Space_JSExported_SetCount (global::System.Int32 value) => global::Bootsharp.Generated.Exports.Space.JSExported.SetCount(value);");
        Contains("""[JSImport("IImported.setCountSerialized", "space")] internal static partial void Space_IImported_SetCount_Serialized (global::System.Int32 value);""");
        Contains("public static void Bootsharp_Generated_Imports_Space_JSImported_SetCount(global::System.Int32 value) => Space_IImported_SetCount_Serialized(value);");
    }

    [Fact]
    public void GeneratesForPropertiesInInstanced ()
    {
        AddAssembly(With(
            """
            public record Info;

            public interface IExported
            {
                Info State { get; set; }
                IExported Exported { get; }
                IImported Imported { set; }
            }

            public interface IImported
            {
                Info State { get; set; }
                IImported Imported { get; }
                IExported Exported { set; }
            }

            public class Class
            {
                [Export] public static IExported GetExported (IImported arg) => default;
                [Import] public static IImported GetImported (IExported arg) => default;
            }
            """));
        Execute();
        Contains("[JSExport] [return: JSMarshalAs<JSType.BigInt>] internal static long Bootsharp_Generated_Exports_JSExported_GetState (int _id) => Serializer.Serialize(Instances.Exported<global::IExported>(_id).State, SerializerContext.Info);");
        Contains("[JSExport] internal static void Bootsharp_Generated_Exports_JSExported_SetState (int _id, [JSMarshalAs<JSType.BigInt>] long value) => Instances.Exported<global::IExported>(_id).State = Serializer.Deserialize(value, SerializerContext.Info);");
        Contains("""[JSImport("IImported.getStateSerialized", "index")] [return: JSMarshalAs<JSType.BigInt>] internal static partial long IImported_GetState_Serialized (int _id);""");
        Contains("[JSExport] internal static int Bootsharp_Generated_Exports_JSExported_GetExported (int _id) => Instances.Export(Instances.Exported<global::IExported>(_id).Exported);");
        Contains("[JSExport] internal static void Bootsharp_Generated_Exports_JSExported_SetImported (int _id, int value) => Instances.Exported<global::IExported>(_id).Imported = Instances.Resolve<global::IImported>(value);");
        Contains("""[JSImport("IImported.getImportedSerialized", "index")] internal static partial int IImported_GetImported_Serialized (int _id);""");
        Contains("public static global::IImported Bootsharp_Generated_Imports_JSImported_GetImported(int _id) => Instances.Resolve<global::IImported>(IImported_GetImported_Serialized(_id));");
        Contains("""[JSImport("IImported.setExportedSerialized", "index")] internal static partial void IImported_SetExported_Serialized (int _id, int value);""");
        Contains("public static void Bootsharp_Generated_Imports_JSImported_SetExported(int _id, global::IExported value) => IImported_SetExported_Serialized(_id, Instances.Export(value));");
    }

    [Fact]
    public void GeneratesForEventsInModules ()
    {
        AddAssembly(With(
            """
            [assembly:Export(typeof(Space.IExported))]
            [assembly:Import(typeof(Space.IImported))]

            namespace Space;

            public record Info;

            public interface IExported { event Action<Info> Evt; }
            public interface IImported { event Action<Info> Evt; }
            """));
        Execute();
        Contains(
            """
                [ModuleInitializer]
                internal static unsafe void Initialize ()
                {
                    global::Bootsharp.Generated.Exports.Space.JSExported.Evt += Handle_Bootsharp_Generated_Exports_Space_JSExported_Evt;
                }
            """);
        Contains("void Handle_Bootsharp_Generated_Exports_Space_JSExported_Evt (global::Space.Info obj) => Space_IExported_BroadcastEvt_Serialized(Serializer.Serialize(obj, SerializerContext.Space_Info));");
        Contains("""[JSImport("IExported.broadcastEvtSerialized", "space")] internal static partial void Space_IExported_BroadcastEvt_Serialized ([JSMarshalAs<JSType.BigInt>] long obj);""");
        Contains("[JSExport] internal static void Bootsharp_Generated_Imports_Space_JSImported_InvokeEvt ([JSMarshalAs<JSType.BigInt>] long obj) => ((global::Bootsharp.Generated.Imports.Space.JSImported)Modules.Imports[typeof(global::Space.IImported)].Instance).InvokeEvt(Serializer.Deserialize(obj, SerializerContext.Space_Info));");
    }

    [Fact]
    public void GeneratesForEventsInInstanced ()
    {
        AddAssembly(With(
            """
            public record Record;

            public interface IExported { event Action<Record, IExported> Changed; }
            public interface IImported { event Action<Record, IImported> Changed; }

            public partial class Class
            {
                [Export] public static IExported GetExported (IImported it) => default;
                [Import] public static IImported GetImported (IExported it) => default;
            }
            """));
        Execute();
        Contains("""[JSImport("IExported.broadcastChangedSerialized", "index")] internal static partial void IExported_BroadcastChanged_Serialized (int _id, [JSMarshalAs<JSType.BigInt>] long arg1, int arg2);""");
        Contains("[JSExport] internal static void Bootsharp_Generated_Imports_JSImported_InvokeChanged (int _id, [JSMarshalAs<JSType.BigInt>] long arg1, int arg2) => ((global::Bootsharp.Generated.Imports.JSImported)Instances.Resolve<global::IImported>(_id)).InvokeChanged(Serializer.Deserialize(arg1, SerializerContext.Record), Instances.Resolve<global::IImported>(arg2));");
    }

    [Fact]
    public void DoesNotEmitStaticEventSubscriptionForInstanceEvents ()
    {
        AddAssembly(With(
            """
            public interface IExportedInstance { event Action? OnChanged; }

            public class Class
            {
                [Export] public static IExportedInstance Get () => default!;
            }
            """));
        Execute();
        DoesNotContain("OnChanged +=");
    }

    [Fact]
    public void DoesNotGenerateForUnsupportedProperties ()
    {
        AddAssembly(With(
            """
            [assembly:Export(typeof(IExportedStatic))]

            public interface IExportedStatic
            {
                int DefaultGet { get => 0; }
                int DefaultSet { set { } }
                int BothDefault { get => 0; set { } }
                int this[int index] { get; set; }
            }

            public interface IExportedInstanced
            {
                int DefaultGet { get => 0; }
                int DefaultSet { set { } }
                int BothDefault { get => 0; set { } }
                int this[int index] { get; set; }
            }

            public class Class
            {
                [Export] public static IExportedInstanced GetExported () => default;
            }
            """));
        Execute();
        DoesNotContain("DefaultGet");
        DoesNotContain("DefaultSet");
        DoesNotContain("BothDefault");
        DoesNotContain("GetItem");
        DoesNotContain("SetItem");
    }

    [Fact]
    public void IgnoresImplementedInterfaceMethods ()
    {
        AddAssembly(With(
            """
            [assembly:Export(typeof(IExportedStatic))]
            [assembly:Import(typeof(IImportedStatic))]

            public interface IExportedStatic { int Foo () => 0; }
            public interface IImportedStatic { int Foo () => 0; }
            public interface IExportedInstanced { int Foo () => 0; }
            public interface IImportedInstanced { int Foo () => 0; }

            public class Class
            {
                [Export] public static IExportedInstanced GetExported () => default;
                [Import] public static IImportedInstanced GetImported () => default;
            }
            """));
        Execute();
        DoesNotContain("Foo");
    }

    [Fact]
    public void DoesNotSerializeTypesThatShouldNotBeSerialized ()
    {
        AddAssembly(With(
            """
            namespace Space;

            public class Class
            {
                [Export] public static Task<Exception> Inv (bool a1, byte a2, char a3, short a4, long a5, int a6, float a7, double a8, nint a9, DateTime a10, DateTimeOffset a11, string a12) => default!;
                [Export] public static Task<DateTime> InvNull (bool? a1, byte? a2, char? a3, short? a4, long? a5, int? a6, float? a7, double? a8, nint? a9, DateTime? a10, DateTimeOffset? a11, string? a12) => default!;
                [Import] public static Task<Exception> Fun (bool a1, byte a2, char a3, short a4, long a5, int a6, float a7, double a8, nint a9, DateTime a10, DateTimeOffset a11, string a12) => Proxies.Get<Func<bool, byte, char, short, long, int, float, double, nint, DateTime, DateTimeOffset, string, Task<Exception>>>("Space.Class.Fun")(a1, a2, a3, a4, a5, a6, a7, a8, a9, a10, a11, a12);
                [Import] public static Task<DateTime> FunNull (bool? a1, byte? a2, char? a3, short? a4, long? a5, int? a6, float? a7, double? a8, nint? a9, DateTime? a10, DateTimeOffset? a11, string? a12) => Proxies.Get<Func<bool?, byte?, char?, short?, long?, int?, float?, double?, nint?, DateTime?, DateTimeOffset?, string?, Task<DateTime>>>("Space.Class.FunNull")(a1, a2, a3, a4, a5, a6, a7, a8, a9, a10, a11, a12);
            }
            """));
        Execute();
        Contains("[JSExport] internal static void Space_Class_Inv (int _taskId, global::System.Boolean a1, global::System.Byte a2, global::System.Char a3, global::System.Int16 a4, [JSMarshalAs<JSType.BigInt>] global::System.Int64 a5, global::System.Int32 a6, global::System.Single a7, global::System.Double a8, global::System.IntPtr a9, [JSMarshalAs<JSType.Date>] global::System.DateTime a10, [JSMarshalAs<JSType.Date>] global::System.DateTimeOffset a11, global::System.String a12)");
        Contains("var _result = await global::Space.Class.Inv(a1, a2, a3, a4, a5, a6, a7, a8, a9, a10, a11, a12);");
        Contains("""[JSImport("Class.invNotify", "space")] internal static partial void Space_Class_Inv_Notify (int _taskId, global::System.Exception _result);""");
        Contains("""[JSImport("Class.invFail", "space")] internal static partial void Space_Class_Inv_Fail (int _taskId, [JSMarshalAs<JSType.BigInt>] long _message);""");
        Contains("[JSExport] internal static void Space_Class_InvNull (int _taskId, global::System.Boolean? a1, global::System.Byte? a2, global::System.Char? a3, global::System.Int16? a4, [JSMarshalAs<JSType.BigInt>] global::System.Int64? a5, global::System.Int32? a6, global::System.Single? a7, global::System.Double? a8, global::System.IntPtr? a9, [JSMarshalAs<JSType.Date>] global::System.DateTime? a10, [JSMarshalAs<JSType.Date>] global::System.DateTimeOffset? a11, global::System.String? a12)");
        Contains("""[JSImport("Class.invNullNotify", "space")] internal static partial void Space_Class_InvNull_Notify (int _taskId, [JSMarshalAs<JSType.Date>] global::System.DateTime _result);""");
        Contains("""[JSImport("Class.funSerialized", "space")] internal static partial void Space_Class_Fun_Serialized (int _taskId, global::System.Boolean a1, global::System.Byte a2, global::System.Char a3, global::System.Int16 a4, [JSMarshalAs<JSType.BigInt>] global::System.Int64 a5, global::System.Int32 a6, global::System.Single a7, global::System.Double a8, global::System.IntPtr a9, [JSMarshalAs<JSType.Date>] global::System.DateTime a10, [JSMarshalAs<JSType.Date>] global::System.DateTimeOffset a11, global::System.String a12);""");
        Contains("[JSExport] internal static void Space_Class_Fun_Complete (int _taskId, global::System.Exception _result) => PendingImports.Take<global::System.Exception>(_taskId).SetResult(_result);");
        Contains("[JSExport] internal static void Space_Class_Fun_Fail (int _taskId, [JSMarshalAs<JSType.BigInt>] long _message) => PendingImports.Take<global::System.Exception>(_taskId).SetException(new JSException(Serializer.Deserialize(_message, Serializer.String)!));");
        Contains("public static global::System.Threading.Tasks.Task<global::System.Exception> Space_Class_Fun (global::System.Boolean a1, global::System.Byte a2, global::System.Char a3, global::System.Int16 a4, global::System.Int64 a5, global::System.Int32 a6, global::System.Single a7, global::System.Double a8, global::System.IntPtr a9, global::System.DateTime a10, global::System.DateTimeOffset a11, global::System.String a12)");
        Contains("var _tcs = new global::System.Threading.Tasks.TaskCompletionSource<global::System.Exception>();");
        Contains("Space_Class_Fun_Serialized(_taskId, a1, a2, a3, a4, a5, a6, a7, a8, a9, a10, a11, a12);");
        Contains("""[JSImport("Class.funNullSerialized", "space")] internal static partial void Space_Class_FunNull_Serialized (int _taskId, global::System.Boolean? a1, global::System.Byte? a2, global::System.Char? a3, global::System.Int16? a4, [JSMarshalAs<JSType.BigInt>] global::System.Int64? a5, global::System.Int32? a6, global::System.Single? a7, global::System.Double? a8, global::System.IntPtr? a9, [JSMarshalAs<JSType.Date>] global::System.DateTime? a10, [JSMarshalAs<JSType.Date>] global::System.DateTimeOffset? a11, global::System.String? a12);""");
        Contains("[JSExport] internal static void Space_Class_FunNull_Complete (int _taskId, [JSMarshalAs<JSType.Date>] global::System.DateTime _result) => PendingImports.Take<global::System.DateTime>(_taskId).SetResult(_result);");
        Contains("public static global::System.Threading.Tasks.Task<global::System.DateTime> Space_Class_FunNull (global::System.Boolean? a1, global::System.Byte? a2, global::System.Char? a3, global::System.Int16? a4, global::System.Int64? a5, global::System.Int32? a6, global::System.Single? a7, global::System.Double? a8, global::System.IntPtr? a9, global::System.DateTime? a10, global::System.DateTimeOffset? a11, global::System.String? a12)");
    }

    [Fact]
    public void SerializesTypesThatShouldBeSerialized ()
    {
        AddAssembly(With(
            """
            namespace Space;

            public record Record;

            public partial class Class
            {
                [Export] public static event Action<Record>? ExpEvt;
                [Import] public static event Action<Record, int>? ImpEvt;
                [Export] public static Record InvA (Record a) => default;
                [Export] public static Task<Record?[]?> InvB (Record?[]? a) => default;
                [Import] public static Record FunA (Record a) => Proxies.Get<Func<Record, Record>>("Space.Class.FunA")(a);
                [Import] public static Task<Record?[]?> FunB (Record?[]? a) => Proxies.Get<Func<Record?[]?, Task<Record?[]?>>>("Space.Class.FunB")(a);
            }
            """));
        Execute();
        Contains("""[JSImport("Class.broadcastExpEvtSerialized", "space")] internal static partial void Space_Class_BroadcastExpEvt_Serialized ([JSMarshalAs<JSType.BigInt>] long obj);""");
        Contains("void Handle_Space_Class_ExpEvt (global::Space.Record obj) => Space_Class_BroadcastExpEvt_Serialized(Serializer.Serialize(obj, SerializerContext.Space_Record));");
        Contains("[JSExport] internal static void Space_Class_InvokeImpEvt ([JSMarshalAs<JSType.BigInt>] long arg1, global::System.Int32 arg2) => global::Space.Class.Bootsharp_Invoke_ImpEvt(Serializer.Deserialize(arg1, SerializerContext.Space_Record), arg2);");
        Contains("[JSExport] [return: JSMarshalAs<JSType.BigInt>] internal static long Space_Class_InvA ([JSMarshalAs<JSType.BigInt>] long a) => Serializer.Serialize(global::Space.Class.InvA(Serializer.Deserialize(a, SerializerContext.Space_Record)), SerializerContext.Space_Record);");
        Contains("[JSExport] internal static void Space_Class_InvB (int _taskId, [JSMarshalAs<JSType.BigInt>] long a)");
        Contains("var _result = Serializer.Serialize(await global::Space.Class.InvB(Serializer.Deserialize(a, SerializerContext.Space_RecordArray)), SerializerContext.Space_RecordArray);");
        Contains("Space_Class_InvB_Notify(_taskId, _result);");
        Contains("""[JSImport("Class.invBNotify", "space")] internal static partial void Space_Class_InvB_Notify (int _taskId, [JSMarshalAs<JSType.BigInt>] long _result);""");
        Contains("""[JSImport("Class.invBFail", "space")] internal static partial void Space_Class_InvB_Fail (int _taskId, [JSMarshalAs<JSType.BigInt>] long _message);""");
        Contains("""[JSImport("Class.funASerialized", "space")] [return: JSMarshalAs<JSType.BigInt>] internal static partial long Space_Class_FunA_Serialized ([JSMarshalAs<JSType.BigInt>] long a);""");
        Contains("public static global::Space.Record Space_Class_FunA (global::Space.Record a) => Serializer.Deserialize(Space_Class_FunA_Serialized(Serializer.Serialize(a, SerializerContext.Space_Record)), SerializerContext.Space_Record);");
        Contains("""[JSImport("Class.funBSerialized", "space")] internal static partial void Space_Class_FunB_Serialized (int _taskId, [JSMarshalAs<JSType.BigInt>] long a);""");
        Contains("[JSExport] internal static void Space_Class_FunB_Complete (int _taskId, [JSMarshalAs<JSType.BigInt>] long _result) => PendingImports.Take<global::Space.Record?[]?>(_taskId).SetResult(Serializer.Deserialize(_result, SerializerContext.Space_RecordArray));");
        Contains("public static global::System.Threading.Tasks.Task<global::Space.Record?[]?> Space_Class_FunB (global::Space.Record?[]? a)");
        Contains("var _tcs = new global::System.Threading.Tasks.TaskCompletionSource<global::Space.Record?[]?>();");
        Contains("Space_Class_FunB_Serialized(_taskId, Serializer.Serialize(a, SerializerContext.Space_RecordArray));");
    }

    [Fact]
    public void RespectsSpacePref ()
    {
        AddAssembly(With(
            """
            [assembly:Preferences(Space = [@"Space", "Foo"])]
            [assembly:Export(typeof(Space.IExported))]
            [assembly:Import(typeof(Space.IImported))]

            namespace Space;

            public interface IExported { void Inv (); }
            public interface IImported { void Fun (); }

            public class Class
            {
                [Export] public static event Action? Evt;
                [Export] public static void Inv () {}
                [Import] public static void Fun () => Proxies.Get<Action>("Class.Fun")();
            }
            """));
        Execute();
        Contains("[JSExport] internal static void Bootsharp_Generated_Exports_Space_JSExported_Inv () => global::Bootsharp.Generated.Exports.Space.JSExported.Inv();");
        Contains("""[JSImport("IImported.funSerialized", "foo")] internal static partial void Bootsharp_Generated_Imports_Space_JSImported_Fun_Serialized ();""");
        Contains("""[JSImport("Class.broadcastEvtSerialized", "foo")] internal static partial void Space_Class_BroadcastEvt_Serialized ();""");
        Contains("void Handle_Space_Class_Evt () => Space_Class_BroadcastEvt_Serialized();");
        Contains("[JSExport] internal static void Space_Class_Inv () => global::Space.Class.Inv();");
        Contains("""[JSImport("Class.funSerialized", "foo")] internal static partial void Space_Class_Fun_Serialized ();""");
    }
}
