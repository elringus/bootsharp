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
        Contains("[JSExport] internal static void DisposeExportedInstance (int id) => Instances.DisposeExported(id);");
        Contains("""[JSImport("instances.disposeImported", "Bootsharp")] internal static partial void DisposeImportedInstance (int id);""");
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
                    global::Library.Registry.Evt += Handle_Library_Registry_Evt;
                    global::Entry.App.Bootsharp_GetName = &Entry_App_GetName;
                    global::Library.Registry.Bootsharp_GetLabel = &Library_Registry_GetLabel;
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
            }
            """));
        Execute();
        Contains("void Handle_Class_ExpEvt () => Class_BroadcastExpEvt_Serialized();");
        Contains("""[JSImport("Class.broadcastExpEvtSerialized", "Bootsharp")] internal static partial void Class_BroadcastExpEvt_Serialized ();""");
        Contains("[JSExport] internal static void Class_InvokeImpEvt () => global::Class.Bootsharp_Invoke_ImpEvt();");
        Contains("[JSExport] internal static void Class_Inv () => global::Class.Inv();");
        Contains("""[JSImport("Class.funSerialized", "Bootsharp")] internal static partial void Class_Fun_Serialized ();""");
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
                }
            }
            namespace SpaceA.SpaceB
            {
                public partial class Class
                {
                    [Import] public static event Action? ImpEvt;
                    [Export] public static void Inv () {}
                    [Import] public static void Fun () => Proxies.Get<Action>("SpaceA.SpaceB.Class.Fun")();
                }
            }
            """));
        Execute();
        Contains("""[JSImport("SpaceA.Class.broadcastExpEvtSerialized", "Bootsharp")] internal static partial void SpaceA_Class_BroadcastExpEvt_Serialized ();""");
        Contains("void Handle_SpaceA_Class_ExpEvt () => SpaceA_Class_BroadcastExpEvt_Serialized();");
        Contains("[JSExport] internal static void SpaceA_Class_Inv () => global::SpaceA.Class.Inv();");
        Contains("""[JSImport("SpaceA.Class.funSerialized", "Bootsharp")] internal static partial void SpaceA_Class_Fun_Serialized ();""");
        Contains("[JSExport] internal static void SpaceA_SpaceB_Class_InvokeImpEvt () => global::SpaceA.SpaceB.Class.Bootsharp_Invoke_ImpEvt();");
        Contains("[JSExport] internal static void SpaceA_SpaceB_Class_Inv () => global::SpaceA.SpaceB.Class.Inv();");
        Contains("""[JSImport("SpaceA.SpaceB.Class.funSerialized", "Bootsharp")] internal static partial void SpaceA_SpaceB_Class_Fun_Serialized ();""");
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
        Contains("[JSExport] [return: JSMarshalAs<JSType.BigInt>] internal static global::System.Int64 Bootsharp_Generated_Exports_Space_JSExported_Inv (global::System.String str, [JSMarshalAs<JSType.BigInt>] global::System.Int64 info) => Serializer.Serialize(global::Bootsharp.Generated.Exports.Space.JSExported.Inv(str, Serializer.Deserialize(info, SerializerContext.Space_Info)), SerializerContext.Space_Info);");
        Contains("""[JSImport("Space.Imported.funSerialized", "Bootsharp")] [return: JSMarshalAs<JSType.BigInt>] internal static partial global::System.Int64 Bootsharp_Generated_Imports_Space_JSImported_Fun_Serialized (global::System.String str, [JSMarshalAs<JSType.BigInt>] global::System.Int64 info);""");
        Contains("public static global::Space.Info Bootsharp_Generated_Imports_Space_JSImported_Fun (global::System.String str, global::Space.Info info) => Serializer.Deserialize(Bootsharp_Generated_Imports_Space_JSImported_Fun_Serialized(str, Serializer.Serialize(info, SerializerContext.Space_Info)), SerializerContext.Space_Info);");
    }

    [Fact]
    public void GeneratesForMethodsInInstancedInterfaces ()
    {
        AddAssembly(With(
            """
            public record Info (string Value);

            public interface IExported { Info Inv (IExported inst, Info info); }
            public interface IImported { Info Fun (IImported inst, Info info); }

            public partial class Class
            {
                [Export] public static Task<IExported> GetExported (IImported inst) => default;
                [Import] public static Task<IImported> GetImported (IExported inst) => default;
            }
            """));
        Execute();
        Contains("[JSExport] [return: JSMarshalAs<JSType.BigInt>] internal static global::System.Int64 Bootsharp_Generated_Exports_JSExported_Inv (global::System.Int32 _id, global::System.Int32 inst, [JSMarshalAs<JSType.BigInt>] global::System.Int64 info) => Serializer.Serialize(Instances.Exported<global::IExported>(_id).Inv(Instances.Exported<global::IExported>(inst), Serializer.Deserialize(info, SerializerContext.Info)), SerializerContext.Info);");
        Contains("""[JSImport("Imported.funSerialized", "Bootsharp")] [return: JSMarshalAs<JSType.BigInt>] internal static partial global::System.Int64 Bootsharp_Generated_Imports_JSImported_Fun_Serialized (global::System.Int32 _id, global::System.Int32 inst, [JSMarshalAs<JSType.BigInt>] global::System.Int64 info);""");
        Contains("public static global::Info Bootsharp_Generated_Imports_JSImported_Fun (global::System.Int32 _id, global::IImported inst, global::Info info) => Serializer.Deserialize(Bootsharp_Generated_Imports_JSImported_Fun_Serialized(_id, ((global::Bootsharp.Generated.Imports.JSImported)inst)._id, Serializer.Serialize(info, SerializerContext.Info)), SerializerContext.Info);");
        Contains("[JSExport] internal static async global::System.Threading.Tasks.Task<global::System.Int32> Class_GetExported (global::System.Int32 inst) => Instances.Export(await global::Class.GetExported(Instances.Import(inst, static id => new global::Bootsharp.Generated.Imports.JSImported(id))));");
        Contains("""[JSImport("Class.getImportedSerialized", "Bootsharp")] internal static partial global::System.Threading.Tasks.Task<global::System.Int32> Class_GetImported_Serialized (global::System.Int32 inst);""");
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
        Contains("[JSExport] [return: JSMarshalAs<JSType.BigInt>] internal static global::System.Int64 Bootsharp_Generated_Exports_Space_JSExported_GetPropertyState () => Serializer.Serialize(global::Bootsharp.Generated.Exports.Space.JSExported.GetPropertyState(), SerializerContext.Space_Info);");
        Contains("[JSExport] internal static void Bootsharp_Generated_Exports_Space_JSExported_SetPropertyState ([JSMarshalAs<JSType.BigInt>] global::System.Int64 value) => global::Bootsharp.Generated.Exports.Space.JSExported.SetPropertyState(Serializer.Deserialize(value, SerializerContext.Space_Info));");
        Contains("""[JSImport("Space.Imported.getPropertyStateSerialized", "Bootsharp")] [return: JSMarshalAs<JSType.BigInt>] internal static partial global::System.Int64 Space_Imported_GetPropertyState_Serialized ();""");
        Contains("public static global::Space.Info Bootsharp_Generated_Imports_Space_JSImported_GetPropertyState() => Serializer.Deserialize(Space_Imported_GetPropertyState_Serialized(), SerializerContext.Space_Info);");
        Contains("""[JSImport("Space.Imported.setPropertyStateSerialized", "Bootsharp")] internal static partial void Space_Imported_SetPropertyState_Serialized ([JSMarshalAs<JSType.BigInt>] global::System.Int64 value);""");
        Contains("public static void Bootsharp_Generated_Imports_Space_JSImported_SetPropertyState(global::Space.Info value) => Space_Imported_SetPropertyState_Serialized(Serializer.Serialize(value, SerializerContext.Space_Info));");
        Contains("[JSExport] internal static global::System.Boolean Bootsharp_Generated_Exports_Space_JSExported_GetPropertyActive () => global::Bootsharp.Generated.Exports.Space.JSExported.GetPropertyActive();");
        Contains("""[JSImport("Space.Imported.getPropertyActiveSerialized", "Bootsharp")] internal static partial global::System.Boolean Space_Imported_GetPropertyActive_Serialized ();""");
        Contains("public static global::System.Boolean Bootsharp_Generated_Imports_Space_JSImported_GetPropertyActive() => Space_Imported_GetPropertyActive_Serialized();");
        Contains("[JSExport] internal static void Bootsharp_Generated_Exports_Space_JSExported_SetPropertyCount (global::System.Int32 value) => global::Bootsharp.Generated.Exports.Space.JSExported.SetPropertyCount(value);");
        Contains("""[JSImport("Space.Imported.setPropertyCountSerialized", "Bootsharp")] internal static partial void Space_Imported_SetPropertyCount_Serialized (global::System.Int32 value);""");
        Contains("public static void Bootsharp_Generated_Imports_Space_JSImported_SetPropertyCount(global::System.Int32 value) => Space_Imported_SetPropertyCount_Serialized(value);");
    }

    [Fact]
    public void GeneratesForPropertiesInInstancedInterfaces ()
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
        Contains("[JSExport] [return: JSMarshalAs<JSType.BigInt>] internal static global::System.Int64 Bootsharp_Generated_Exports_JSExported_GetPropertyState (global::System.Int32 _id) => Serializer.Serialize(Instances.Exported<global::IExported>(_id).State, SerializerContext.Info);");
        Contains("[JSExport] internal static void Bootsharp_Generated_Exports_JSExported_SetPropertyState (global::System.Int32 _id, [JSMarshalAs<JSType.BigInt>] global::System.Int64 value) => Instances.Exported<global::IExported>(_id).State = Serializer.Deserialize(value, SerializerContext.Info);");
        Contains("""[JSImport("Imported.getPropertyStateSerialized", "Bootsharp")] [return: JSMarshalAs<JSType.BigInt>] internal static partial global::System.Int64 Imported_GetPropertyState_Serialized (global::System.Int32 _id);""");
        Contains("[JSExport] internal static global::System.Int32 Bootsharp_Generated_Exports_JSExported_GetPropertyExported (global::System.Int32 _id) => Instances.Export(Instances.Exported<global::IExported>(_id).Exported);");
        Contains("[JSExport] internal static void Bootsharp_Generated_Exports_JSExported_SetPropertyImported (global::System.Int32 _id, global::System.Int32 value) => Instances.Exported<global::IExported>(_id).Imported = Instances.Import(value, static id => new global::Bootsharp.Generated.Imports.JSImported(id));");
        Contains("""[JSImport("Imported.getPropertyImportedSerialized", "Bootsharp")] internal static partial global::System.Int32 Imported_GetPropertyImported_Serialized (global::System.Int32 _id);""");
        Contains("public static global::IImported Bootsharp_Generated_Imports_JSImported_GetPropertyImported(global::System.Int32 _id) => Instances.Import(Imported_GetPropertyImported_Serialized(_id), static id => new global::Bootsharp.Generated.Imports.JSImported(id));");
        Contains("""[JSImport("Imported.setPropertyExportedSerialized", "Bootsharp")] internal static partial void Imported_SetPropertyExported_Serialized (global::System.Int32 _id, global::System.Int32 value);""");
        Contains("public static void Bootsharp_Generated_Imports_JSImported_SetPropertyExported(global::System.Int32 _id, global::IExported value) => Imported_SetPropertyExported_Serialized(_id, Instances.Export(value));");
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
        Contains("void Handle_Bootsharp_Generated_Exports_Space_JSExported_Evt (global::Space.Info obj) => Space_Exported_BroadcastEvt_Serialized(Serializer.Serialize(obj, SerializerContext.Space_Info));");
        Contains("""[JSImport("Space.Exported.broadcastEvtSerialized", "Bootsharp")] internal static partial void Space_Exported_BroadcastEvt_Serialized ([JSMarshalAs<JSType.BigInt>] global::System.Int64 obj);""");
        Contains("[JSExport] internal static void Bootsharp_Generated_Imports_Space_JSImported_InvokeEvt ([JSMarshalAs<JSType.BigInt>] global::System.Int64 obj) => ((global::Bootsharp.Generated.Imports.Space.JSImported)Modules.Imports[typeof(global::Space.IImported)].Instance).InvokeEvt(Serializer.Deserialize(obj, SerializerContext.Space_Info));");
    }

    [Fact]
    public void GeneratesForEventsInInstancedInterfaces ()
    {
        AddAssembly(With(
            """
            public record Record;

            public interface IExported { event Action<Record, IExported> Changed; }
            public interface IImported { event Action<Record, IImported> Changed; }

            public partial class Class
            {
                [Export] public static IExported GetExported (IImported inst) => default;
                [Import] public static IImported GetImported (IExported inst) => default;
            }
            """));
        Execute();
        Contains(
            """
                private static int Register (global::IExported instance) => Instances.Export(instance, static (_id, instance) => {
                    instance.Changed += HandleChanged;
                    return () => {
                        instance.Changed -= HandleChanged;
                    };

                    void HandleChanged (global::Record arg1, global::IExported arg2) => Exported_BroadcastChanged_Serialized(_id, Serializer.Serialize(arg1, SerializerContext.Record), Register(arg2));
                });
            """);
        Contains("""[JSImport("Exported.broadcastChangedSerialized", "Bootsharp")] internal static partial void Exported_BroadcastChanged_Serialized (global::System.Int32 _id, [JSMarshalAs<JSType.BigInt>] global::System.Int64 arg1, global::System.Int32 arg2);""");
        Contains("[JSExport] internal static void Bootsharp_Generated_Imports_JSImported_InvokeChanged (global::System.Int32 _id, [JSMarshalAs<JSType.BigInt>] global::System.Int64 arg1, global::System.Int32 arg2) => Instances.Import(_id, static id => new global::Bootsharp.Generated.Imports.JSImported(id)).InvokeChanged(Serializer.Deserialize(arg1, SerializerContext.Record), Instances.Import(arg2, static id => new global::Bootsharp.Generated.Imports.JSImported(id)));");
    }

    [Fact]
    public void DoesNotGenerateForUnsupportedProperties ()
    {
        AddAssembly(With(
            """
            [assembly:Export(typeof(IExportedStatic))]

            public interface IExportedStatic
            {
                int Ignored { get => 0; }
                int IgnoredToo { set { } }
                int this[int index] { get; set; }
            }

            public interface IExportedInstanced
            {
                int Ignored { get => 0; }
                int IgnoredToo { set { } }
                int this[int index] { get; set; }
            }

            public class Class
            {
                [Export] public static IExportedInstanced GetExported () => default;
            }
            """));
        Execute();
        DoesNotContain("Ignored");
        DoesNotContain("IgnoredToo");
        DoesNotContain("GetPropertyItem");
        DoesNotContain("SetPropertyItem");
    }

    [Fact]
    public void DoesNotEmitDuplicateModuleRegistrations ()
    {
        AddAssembly(With(
            """
            public interface IExported
            {
                event Action? Changed;
                event Action<string>? Done;
            }

            public class Class
            {
                [Export] public static IExported GetExported () => default;
            }
            """));
        Execute();
        Once(@"private static int Register \(global::IExported instance\)");
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
    public void DoesntSerializeTypesThatShouldNotBeSerialized ()
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
        Contains("[JSExport] internal static global::System.Threading.Tasks.Task<global::System.Exception> Space_Class_Inv (global::System.Boolean a1, global::System.Byte a2, global::System.Char a3, global::System.Int16 a4, [JSMarshalAs<JSType.BigInt>] global::System.Int64 a5, global::System.Int32 a6, global::System.Single a7, global::System.Double a8, global::System.IntPtr a9, [JSMarshalAs<JSType.Date>] global::System.DateTime a10, [JSMarshalAs<JSType.Date>] global::System.DateTimeOffset a11, global::System.String a12) => global::Space.Class.Inv(a1, a2, a3, a4, a5, a6, a7, a8, a9, a10, a11, a12);");
        Contains("[JSExport] [return: JSMarshalAs<JSType.Promise<JSType.Date>>] internal static global::System.Threading.Tasks.Task<global::System.DateTime> Space_Class_InvNull (global::System.Boolean? a1, global::System.Byte? a2, global::System.Char? a3, global::System.Int16? a4, [JSMarshalAs<JSType.BigInt>] global::System.Int64? a5, global::System.Int32? a6, global::System.Single? a7, global::System.Double? a8, global::System.IntPtr? a9, [JSMarshalAs<JSType.Date>] global::System.DateTime? a10, [JSMarshalAs<JSType.Date>] global::System.DateTimeOffset? a11, global::System.String? a12) => global::Space.Class.InvNull(a1, a2, a3, a4, a5, a6, a7, a8, a9, a10, a11, a12);");
        Contains("""[JSImport("Space.Class.funSerialized", "Bootsharp")] internal static partial global::System.Threading.Tasks.Task<global::System.Exception> Space_Class_Fun_Serialized (global::System.Boolean a1, global::System.Byte a2, global::System.Char a3, global::System.Int16 a4, [JSMarshalAs<JSType.BigInt>] global::System.Int64 a5, global::System.Int32 a6, global::System.Single a7, global::System.Double a8, global::System.IntPtr a9, [JSMarshalAs<JSType.Date>] global::System.DateTime a10, [JSMarshalAs<JSType.Date>] global::System.DateTimeOffset a11, global::System.String a12);""");
        Contains("public static global::System.Threading.Tasks.Task<global::System.DateTime> Space_Class_FunNull (global::System.Boolean? a1, global::System.Byte? a2, global::System.Char? a3, global::System.Int16? a4, global::System.Int64? a5, global::System.Int32? a6, global::System.Single? a7, global::System.Double? a8, global::System.IntPtr? a9, global::System.DateTime? a10, global::System.DateTimeOffset? a11, global::System.String? a12) => Space_Class_FunNull_Serialized(a1, a2, a3, a4, a5, a6, a7, a8, a9, a10, a11, a12);");
        Contains("""[JSImport("Space.Class.funNullSerialized", "Bootsharp")] [return: JSMarshalAs<JSType.Promise<JSType.Date>>] internal static partial global::System.Threading.Tasks.Task<global::System.DateTime> Space_Class_FunNull_Serialized (global::System.Boolean? a1, global::System.Byte? a2, global::System.Char? a3, global::System.Int16? a4, [JSMarshalAs<JSType.BigInt>] global::System.Int64? a5, global::System.Int32? a6, global::System.Single? a7, global::System.Double? a8, global::System.IntPtr? a9, [JSMarshalAs<JSType.Date>] global::System.DateTime? a10, [JSMarshalAs<JSType.Date>] global::System.DateTimeOffset? a11, global::System.String? a12);""");
        Contains("public static global::System.Threading.Tasks.Task<global::System.DateTime> Space_Class_FunNull (global::System.Boolean? a1, global::System.Byte? a2, global::System.Char? a3, global::System.Int16? a4, global::System.Int64? a5, global::System.Int32? a6, global::System.Single? a7, global::System.Double? a8, global::System.IntPtr? a9, global::System.DateTime? a10, global::System.DateTimeOffset? a11, global::System.String? a12) => Space_Class_FunNull_Serialized(a1, a2, a3, a4, a5, a6, a7, a8, a9, a10, a11, a12);");
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
        Contains("""[JSImport("Space.Class.broadcastExpEvtSerialized", "Bootsharp")] internal static partial void Space_Class_BroadcastExpEvt_Serialized ([JSMarshalAs<JSType.BigInt>] global::System.Int64 obj);""");
        Contains("void Handle_Space_Class_ExpEvt (global::Space.Record obj) => Space_Class_BroadcastExpEvt_Serialized(Serializer.Serialize(obj, SerializerContext.Space_Record));");
        Contains("[JSExport] internal static void Space_Class_InvokeImpEvt ([JSMarshalAs<JSType.BigInt>] global::System.Int64 arg1, global::System.Int32 arg2) => global::Space.Class.Bootsharp_Invoke_ImpEvt(Serializer.Deserialize(arg1, SerializerContext.Space_Record), arg2);");
        Contains("[JSExport] [return: JSMarshalAs<JSType.BigInt>] internal static global::System.Int64 Space_Class_InvA ([JSMarshalAs<JSType.BigInt>] global::System.Int64 a) => Serializer.Serialize(global::Space.Class.InvA(Serializer.Deserialize(a, SerializerContext.Space_Record)), SerializerContext.Space_Record);");
        Contains("[JSExport] [return: JSMarshalAs<JSType.Promise<JSType.BigInt>>] internal static async global::System.Threading.Tasks.Task<global::System.Int64> Space_Class_InvB ([JSMarshalAs<JSType.BigInt>] global::System.Int64 a) => Serializer.Serialize(await global::Space.Class.InvB(Serializer.Deserialize(a, SerializerContext.Space_RecordArray)), SerializerContext.Space_RecordArray);");
        Contains("""[JSImport("Space.Class.funASerialized", "Bootsharp")] [return: JSMarshalAs<JSType.BigInt>] internal static partial global::System.Int64 Space_Class_FunA_Serialized ([JSMarshalAs<JSType.BigInt>] global::System.Int64 a);""");
        Contains("public static global::Space.Record Space_Class_FunA (global::Space.Record a) => Serializer.Deserialize(Space_Class_FunA_Serialized(Serializer.Serialize(a, SerializerContext.Space_Record)), SerializerContext.Space_Record);");
        Contains("""[JSImport("Space.Class.funBSerialized", "Bootsharp")] [return: JSMarshalAs<JSType.Promise<JSType.BigInt>>] internal static partial global::System.Threading.Tasks.Task<global::System.Int64> Space_Class_FunB_Serialized ([JSMarshalAs<JSType.BigInt>] global::System.Int64 a);""");
        Contains("public static async global::System.Threading.Tasks.Task<global::Space.Record?[]?> Space_Class_FunB (global::Space.Record?[]? a) => Serializer.Deserialize(await Space_Class_FunB_Serialized(Serializer.Serialize(a, SerializerContext.Space_RecordArray)), SerializerContext.Space_RecordArray);");
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
        Contains("""[JSImport("Foo.Imported.funSerialized", "Bootsharp")] internal static partial void Bootsharp_Generated_Imports_Space_JSImported_Fun_Serialized ();""");
        Contains("""[JSImport("Foo.Class.broadcastEvtSerialized", "Bootsharp")] internal static partial void Foo_Class_BroadcastEvt_Serialized ();""");
        Contains("void Handle_Space_Class_Evt () => Foo_Class_BroadcastEvt_Serialized();");
        Contains("[JSExport] internal static void Space_Class_Inv () => global::Space.Class.Inv();");
        Contains("""[JSImport("Foo.Class.funSerialized", "Bootsharp")] internal static partial void Space_Class_Fun_Serialized ();""");
    }
}
