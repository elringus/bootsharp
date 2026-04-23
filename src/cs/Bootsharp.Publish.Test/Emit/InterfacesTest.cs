namespace Bootsharp.Publish.Test;

public class InterfacesTest : EmitTest
{
    protected override string TestedContent => GeneratedInterfaces;

    [Fact]
    public void GeneratesImplementationForExportedStaticInterface ()
    {
        AddAssembly(With(
            """
            [assembly:Export(typeof(IExported))]

            public record Record;

            public interface IExported
            {
                event Action<Record?> OnRecordChanged;

                Record? Record { get; set; }

                void Inv (string? a);
                Task InvAsync ();
                Record? InvRecord ();
                Task<string> InvAsyncResult ();
                string[] InvArray (int[] a);
            }
            """));
        Execute();
        Contains(
            """
            namespace Bootsharp.Generated
            {
                internal static class InterfaceRegistrations
                {
                    [System.Runtime.CompilerServices.ModuleInitializer]
                    internal static void RegisterInterfaces ()
                    {
                        Interfaces.Register(typeof(Bootsharp.Generated.Exports.JSExported), new ExportInterface(typeof(global::IExported), handler => new Bootsharp.Generated.Exports.JSExported((global::IExported)handler)));
                    }
                }
            }

            namespace Bootsharp.Generated.Exports
            {
                public class JSExported
                {
                    private static global::IExported handler = null!;

                    public JSExported (global::IExported handler)
                    {
                        JSExported.handler = handler;
                        handler.OnRecordChanged += OnRecordChanged.Invoke;
                    }

                    [Export] public static event global::System.Action<global::Record?> OnRecordChanged;
                    [Export] public static global::Record? GetPropertyRecord () => handler.Record;
                    [Export] public static void SetPropertyRecord (global::Record? value) => handler.Record = value;
                    [Export] public static void Inv (global::System.String? a) => handler.Inv(a);
                    [Export] public static global::System.Threading.Tasks.Task InvAsync () => handler.InvAsync();
                    [Export] public static global::Record? InvRecord () => handler.InvRecord();
                    [Export] public static global::System.Threading.Tasks.Task<global::System.String> InvAsyncResult () => handler.InvAsyncResult();
                    [Export] public static global::System.String[] InvArray (global::System.Int32[] a) => handler.InvArray(a);
                }
            }
            """);
    }

    [Fact]
    public void GeneratesImplementationForImportedStaticInterface ()
    {
        AddAssembly(With(
            """
            [assembly:Import(typeof(IImported))]

            public record Record;

            public interface IImported
            {
                event Action<Record?> OnRecordChanged;

                Record? Record { get; set; }

                void Inv (string? a);
                Task InvAsync ();
                Record? InvRecord ();
                Task<string> InvAsyncResult ();
                string[] InvArray (int[] a);
            }
            """));
        Execute();
        Contains(
            """
            namespace Bootsharp.Generated
            {
                internal static class InterfaceRegistrations
                {
                    [System.Runtime.CompilerServices.ModuleInitializer]
                    internal static void RegisterInterfaces ()
                    {
                        Interfaces.Register(typeof(global::IImported), new ImportInterface(new Bootsharp.Generated.Imports.JSImported()));
                    }
                }
            }

            namespace Bootsharp.Generated.Imports
            {
                public class JSImported : global::IImported
                {
                    public event global::System.Action<global::Record?> OnRecordChanged;
                    internal void InvokeOnRecordChanged (global::Record? obj) => OnRecordChanged?.Invoke(obj);
                    global::Record? global::IImported.Record
                    {
                        get => global::Bootsharp.Generated.Interop.Bootsharp_Generated_Imports_JSImported_GetPropertyRecord();
                        set => global::Bootsharp.Generated.Interop.Bootsharp_Generated_Imports_JSImported_SetPropertyRecord(value);
                    }
                    void global::IImported.Inv (global::System.String? a) => global::Bootsharp.Generated.Interop.Bootsharp_Generated_Imports_JSImported_Inv(a);
                    global::System.Threading.Tasks.Task global::IImported.InvAsync () => global::Bootsharp.Generated.Interop.Bootsharp_Generated_Imports_JSImported_InvAsync();
                    global::Record? global::IImported.InvRecord () => global::Bootsharp.Generated.Interop.Bootsharp_Generated_Imports_JSImported_InvRecord();
                    global::System.Threading.Tasks.Task<global::System.String> global::IImported.InvAsyncResult () => global::Bootsharp.Generated.Interop.Bootsharp_Generated_Imports_JSImported_InvAsyncResult();
                    global::System.String[] global::IImported.InvArray (global::System.Int32[] a) => global::Bootsharp.Generated.Interop.Bootsharp_Generated_Imports_JSImported_InvArray(a);
                }
            }
            """);
    }

    [Fact]
    public void GeneratesImplementationForImportedInstanceInterface ()
    {
        AddAssembly(With(
            """
            public record Record;

            public interface IImported
            {
                event Action<Record?> OnRecordChanged;

                Record? Record { get; set; }

                void Fun (string arg);
            }

            public class Class
            {
                [Import] public static IImported GetImported () => Proxies.Get<Func<IImported>>("Class.GetImported")();
            }
            """));
        Execute();
        Contains(
            """
            namespace Bootsharp.Generated.Imports
            {
                public class JSImported (global::System.Int32 id) : global::IImported
                {
                    internal readonly global::System.Int32 _id = id;

                    ~JSImported()
                    {
                        global::Bootsharp.Instances.DisposeImported(_id);
                        global::Bootsharp.Generated.Interop.DisposeImportedInstance(_id);
                    }

                    public event global::System.Action<global::Record?> OnRecordChanged;
                    internal void InvokeOnRecordChanged (global::Record? obj) => OnRecordChanged?.Invoke(obj);
                    global::Record? global::IImported.Record
                    {
                        get => global::Bootsharp.Generated.Interop.Bootsharp_Generated_Imports_JSImported_GetPropertyRecord(_id);
                        set => global::Bootsharp.Generated.Interop.Bootsharp_Generated_Imports_JSImported_SetPropertyRecord(_id, value);
                    }
                    void global::IImported.Fun (global::System.String arg) => global::Bootsharp.Generated.Interop.Bootsharp_Generated_Imports_JSImported_Fun(_id, arg);
                }
            }
            """);
    }

    [Fact]
    public void DoesNotGenerateImplementationForExportedInstanceInterface ()
    {
        AddAssembly(With(
            """
            public record Record;

            public interface IExported
            {
                event Action<Record?> OnRecordChanged;

                Record? Record { get; set; }

                void Fun (string arg);
            }

            public class Class
            {
                [Export] public static IExported GetExported () => default;
            }
            """));
        Execute();
        DoesNotContain("JSExported");
    }

    [Fact]
    public void RespectsInterfaceNamespace ()
    {
        AddAssembly(With(
            """
            [assembly:Export(typeof(Space.IExported))]
            [assembly:Import(typeof(Space.IImported))]

            namespace Space;

            public record Record;

            public interface IExported { void Inv (Record a); }
            public interface IImported { void Fun (Record a); }
            """));
        Execute();
        Contains(
            """
            namespace Bootsharp.Generated
            {
                internal static class InterfaceRegistrations
                {
                    [System.Runtime.CompilerServices.ModuleInitializer]
                    internal static void RegisterInterfaces ()
                    {
                        Interfaces.Register(typeof(Bootsharp.Generated.Exports.Space.JSExported), new ExportInterface(typeof(global::Space.IExported), handler => new Bootsharp.Generated.Exports.Space.JSExported((global::Space.IExported)handler)));
                        Interfaces.Register(typeof(global::Space.IImported), new ImportInterface(new Bootsharp.Generated.Imports.Space.JSImported()));
                    }
                }
            }

            namespace Bootsharp.Generated.Exports.Space
            {
                public class JSExported
                {
                    private static global::Space.IExported handler = null!;

                    public JSExported (global::Space.IExported handler)
                    {
                        JSExported.handler = handler;
                    }

                    [Export] public static void Inv (global::Space.Record a) => handler.Inv(a);
                }
            }

            namespace Bootsharp.Generated.Imports.Space
            {
                public class JSImported : global::Space.IImported
                {
                    void global::Space.IImported.Fun (global::Space.Record a) => global::Bootsharp.Generated.Interop.Bootsharp_Generated_Imports_Space_JSImported_Fun(a);
                }
            }
            """);
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
}
