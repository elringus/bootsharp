namespace Bootsharp.Publish.Test;

public class ModulesTest : EmitTest
{
    protected override string TestedContent => GeneratedModules;

    [Fact]
    public void GeneratesExportedInterfaceModule ()
    {
        AddAssembly(With(
            """
            [assembly:Export(typeof(IExported))]

            public record Record;

            public interface IExported
            {
                delegate void SomethingChanged();

                event Action<Record?> OnRecordChanged;
                event SomethingChanged OnSomethingChanged;

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
                internal static class ModuleRegistrations
                {
                    [System.Runtime.CompilerServices.ModuleInitializer]
                    internal static void RegisterModules ()
                    {
                        Modules.Register(typeof(Bootsharp.Generated.Exports.JSExported), new ExportModule(typeof(global::IExported), handler => new Bootsharp.Generated.Exports.JSExported((global::IExported)handler)));
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
                        handler.OnSomethingChanged += OnSomethingChanged.Invoke;
                    }

                    [Export] public static event global::System.Action<global::Record?> OnRecordChanged;
                    [Export] public static event global::IExported.SomethingChanged OnSomethingChanged;
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
    public void GeneratesExportedClassModule ()
    {
        AddAssembly(With(
            """
            [assembly:Export(typeof(Exported))]

            public record Record;

            public class Exported
            {
                public delegate void SomethingChanged();

                public event Action<Record?> OnRecordChanged;
                public event SomethingChanged OnSomethingChanged;

                public Record? Record { get; set; }

                public virtual void Inv (string? a) {}
                public Task InvAsync () => Task.CompletedTask;
                public Record? InvRecord () => null;
                public Task<string> InvAsyncResult () => Task.FromResult("");
                public string[] InvArray (int[] a) => [];
            }
            """));
        Execute();
        Contains(
            """
            namespace Bootsharp.Generated
            {
                internal static class ModuleRegistrations
                {
                    [System.Runtime.CompilerServices.ModuleInitializer]
                    internal static void RegisterModules ()
                    {
                        Modules.Register(typeof(Bootsharp.Generated.Exports.JSExported), new ExportModule(typeof(global::Exported), handler => new Bootsharp.Generated.Exports.JSExported((global::Exported)handler)));
                    }
                }
            }

            namespace Bootsharp.Generated.Exports
            {
                public class JSExported
                {
                    private static global::Exported handler = null!;

                    public JSExported (global::Exported handler)
                    {
                        JSExported.handler = handler;
                        handler.OnRecordChanged += OnRecordChanged.Invoke;
                        handler.OnSomethingChanged += OnSomethingChanged.Invoke;
                    }

                    [Export] public static event global::System.Action<global::Record?> OnRecordChanged;
                    [Export] public static event global::Exported.SomethingChanged OnSomethingChanged;
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
    public void DoesNotGenerateExportedStaticClassModule ()
    {
        AddAssembly(With(
            """
            [assembly:Export(typeof(StaticExported))]

            public static class StaticExported
            {
                public static void Inv () {}
            }
            """));
        Execute();
        DoesNotContain("JSStaticExported");
    }

    [Fact]
    public void GeneratesImportedInterfaceModule ()
    {
        AddAssembly(With(
            """
            [assembly:Import(typeof(IImported))]

            public record Record;

            public interface IImported
            {
                delegate void SomethingChanged();

                event Action<Record?> OnRecordChanged;
                event SomethingChanged OnSomethingChanged;

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
                internal static class ModuleRegistrations
                {
                    [System.Runtime.CompilerServices.ModuleInitializer]
                    internal static void RegisterModules ()
                    {
                        Modules.Register(typeof(global::IImported), new ImportModule(new Bootsharp.Generated.Imports.JSImported()));
                    }
                }
            }

            namespace Bootsharp.Generated.Imports
            {
                public class JSImported : global::IImported
                {
                    public event global::System.Action<global::Record?> OnRecordChanged;
                    internal void InvokeOnRecordChanged (global::Record? obj) => OnRecordChanged?.Invoke(obj);
                    public event global::IImported.SomethingChanged OnSomethingChanged;
                    internal void InvokeOnSomethingChanged () => OnSomethingChanged?.Invoke();
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
    public void DoesNotGenerateImportedClassModule ()
    {
        AddAssembly(With(
            """
            [assembly:Import(typeof(Imported))]

            public class Imported
            {
                public void Inv () {}
            }
            """));
        Execute();
        DoesNotContain("JSImported");
    }

    [Fact]
    public void RespectsModuleNamespace ()
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
                internal static class ModuleRegistrations
                {
                    [System.Runtime.CompilerServices.ModuleInitializer]
                    internal static void RegisterModules ()
                    {
                        Modules.Register(typeof(Bootsharp.Generated.Exports.Space.JSExported), new ExportModule(typeof(global::Space.IExported), handler => new Bootsharp.Generated.Exports.Space.JSExported((global::Space.IExported)handler)));
                        Modules.Register(typeof(global::Space.IImported), new ImportModule(new Bootsharp.Generated.Imports.Space.JSImported()));
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
            [assembly:Export(typeof(IExported))]
            [assembly:Import(typeof(IImported))]

            public interface IExported { int Foo () => 0; }
            public interface IImported { int Foo () => 0; }
            """));
        Execute();
        DoesNotContain("Foo");
    }

    [Fact]
    public void IgnoresStaticMembersOnExportedClassModule ()
    {
        AddAssembly(With(
            """
            [assembly:Export(typeof(Exported))]

            public class Exported
            {
                public static void StaticMethod () {}
                public void Inst () {}
            }
            """));
        Execute();
        DoesNotContain("StaticMethod");
    }
}
