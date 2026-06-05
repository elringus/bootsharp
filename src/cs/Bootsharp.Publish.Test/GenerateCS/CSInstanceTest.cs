namespace Bootsharp.Publish.Test;

public class CSInstanceTest : GenerateCSTest
{
    protected override string TestedContent => GeneratedInstances;

    [Fact]
    public void GeneratesImportedInstanceInterface ()
    {
        AddAssembly(With(
            """
            public record Record;

            public interface IImported
            {
                delegate void SomethingChanged();

                event Action<Record?> OnRecordChanged;
                event SomethingChanged OnSomethingChanged;

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
            public sealed class JS_Import_IImported (int id) : global::Bootsharp.JSProxy(id), global::IImported
            {
                ~JS_Import_IImported() => Instances.DisposeImported(_id);

                public event global::System.Action<global::Record?> OnRecordChanged;
                internal void InvokeOnRecordChanged (global::Record? obj) => OnRecordChanged?.Invoke(obj);
                public event global::IImported.SomethingChanged OnSomethingChanged;
                internal void InvokeOnSomethingChanged () => OnSomethingChanged?.Invoke();
                global::Record? global::IImported.Record
                {
                    get => global::Bootsharp.Generated.Interop.JS_Import_IImported_GetRecord(_id);
                    set => global::Bootsharp.Generated.Interop.JS_Import_IImported_SetRecord(_id, value);
                }
                void global::IImported.Fun (global::System.String arg) => global::Bootsharp.Generated.Interop.JS_Import_IImported_Fun(_id, arg);
            }
            """);
    }

    [Fact]
    public void DoesNotGenerateExportedInstanceInterface ()
    {
        AddAssembly(With(
            """
            public record Record;

            public interface IExported
            {
                delegate void SomethingChanged();

                event Action<Record?> OnRecordChanged;
                event SomethingChanged OnSomethingChanged;

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
    public void IgnoresImplementedInterfaceMethods ()
    {
        AddAssembly(With(
            """
            public interface IExported { int Foo () => 0; }
            public interface IImported { int Foo () => 0; }

            public class Class
            {
                [Export] public static IExported GetExported () => default;
                [Import] public static IExported GetImported () => default;
            }
            """));
        Execute();
        DoesNotContain("Foo");
    }

    [Fact]
    public void GeneratesSpecializedExportsForInstancesWithEvents ()
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
        Contains(
            """
                internal static int Export (global::IExported it) => Export(it, static (_id, it) => {
                    it.Changed += HandleChanged;
                    return () => {
                        it.Changed -= HandleChanged;
                    };

                    void HandleChanged (global::Record arg1, global::IExported arg2) => Interop.JS_Export_IExported_BroadcastChanged_Serialized(_id, Serializer.Serialize(arg1, SerializerContext.Record), Instances.Export(arg2));
                });
            """);
    }

    [Fact]
    public void DoesNotGenerateDuplicateSpecializedExports ()
    {
        AddAssembly(With(
            """
            public interface IBi
            {
                event Action? Changed;
                event Action<string>? Done;
            }

            public class Class
            {
                [Export] public static IBi GetExported () => default!;
                [Import] public static IBi GetImported () => default!;
            }
            """));
        Execute();
        Once(@"internal static int Export \(global::IBi it\)");
    }

    [Fact]
    public void GeneratesImportProxyForBidirectionalProperty ()
    {
        AddAssembly(With(
            """
            [assembly:Export(typeof(IModule))]

            public interface IInstanced;
            public interface IModule { IInstanced Item { get; set; } }
            """));
        Execute();
        Contains("public sealed class JS_Import_IInstanced (int id) : global::Bootsharp.JSProxy(id), global::IInstanced");
    }

    [Fact]
    public void GeneratesProxyForImportedDelegates ()
    {
        AddAssembly(With(
            """
            public delegate void Notify (string msg);

            public class Class
            {
                [Import] public static System.Action GetAction () => default!;
                [Import] public static System.Func<int, string> GetFunc () => default!;
                [Import] public static Notify GetNotify () => default!;
            }
            """));
        Execute();
        Contains("Instances.RegisterImport(typeof(global::System.Action), static id => new global::System.Action(new global::Bootsharp.Generated.JS_Import_System_Action(id).Invoke));");
        Contains("Instances.RegisterImport(typeof(global::System.Func<global::System.Int32, global::System.String>), static id => new global::System.Func<global::System.Int32, global::System.String>(new global::Bootsharp.Generated.JS_Import_System_Func_Of_System_Int32_And_System_String(id).Invoke));");
        Contains("Instances.RegisterImport(typeof(global::Notify), static id => new global::Notify(new global::Bootsharp.Generated.JS_Import_Notify(id).Invoke));");
        Contains(
            """
            public sealed class JS_Import_System_Action (int id) : global::Bootsharp.JSProxy(id)
            {
                ~JS_Import_System_Action() => Instances.DisposeImported(_id);

                public void Invoke () => global::Bootsharp.Generated.Interop.JS_Import_System_Action_Invoke(_id);
            }
            """);
        Contains(
            """
            public sealed class JS_Import_System_Func_Of_System_Int32_And_System_String (int id) : global::Bootsharp.JSProxy(id)
            {
                ~JS_Import_System_Func_Of_System_Int32_And_System_String() => Instances.DisposeImported(_id);

                public global::System.String Invoke (global::System.Int32 arg) => global::Bootsharp.Generated.Interop.JS_Import_System_Func_Of_System_Int32_And_System_String_Invoke(_id, arg);
            }
            """);
        Contains(
            """
            public sealed class JS_Import_Notify (int id) : global::Bootsharp.JSProxy(id)
            {
                ~JS_Import_Notify() => Instances.DisposeImported(_id);

                public void Invoke (global::System.String msg) => global::Bootsharp.Generated.Interop.JS_Import_Notify_Invoke(_id, msg);
            }
            """);
    }

    [Fact]
    public void DoesNotGenerateProxyForExportedDelegates ()
    {
        AddAssembly(WithClass("[Export] public static Action GetAction () => default!;"));
        Execute();
        DoesNotContain("Invoke () =>");
    }

    [Fact]
    public void GeneratesForCustomSpecialization ()
    {
        AddAssembly(With(
            """
            public class Custom
            {
                public event Action ErasedEvent;
                public string ErasedProperty { get; set; }
                public string ErasedMethod () => "";
            }

            [SpecializeImport(typeof(Custom))]
            public abstract class CustomImport (int id) : SpecializedImport(id)
            {
                public abstract event Action AddedEvent;
                public abstract string AddedProperty { get; set; }
                public abstract string AddedMethod ();
            }

            [SpecializeExport(typeof(Custom))]
            public sealed class CustomExport (Custom it) : SpecializedExport(it)
            {
                public event Action AddedEvent;
                public string AddedProperty { get; set; }
                public string AddedMethod () => "";
            }

            public class Class
            {
                [Export] public static Custom Foo (Custom it) => default!;
            }
            """));
        Execute();
        Contains("Instances.RegisterImport(typeof(global::Custom), static id => new global::Bootsharp.Generated.JS_Import_Custom(id));");
        Contains(
            """
            public sealed class JS_Import_Custom (int id) : global::CustomImport(id)
            {
                ~JS_Import_Custom() => Instances.DisposeImported(_id);

                public override event global::System.Action AddedEvent;
                internal void InvokeAddedEvent () => AddedEvent?.Invoke();
                public override global::System.String AddedProperty
                {
                    get => global::Bootsharp.Generated.Interop.JS_Import_Custom_GetAddedProperty(_id);
                    set => global::Bootsharp.Generated.Interop.JS_Import_Custom_SetAddedProperty(_id, value);
                }
                public override global::System.String AddedMethod () => global::Bootsharp.Generated.Interop.JS_Import_Custom_AddedMethod(_id);
            }
            """);
        Contains(
            """
                internal static int Export (global::Custom it) => Export(new global::CustomExport(it), static (_id, it) => {
                    it.AddedEvent += HandleAddedEvent;
                    return () => {
                        it.AddedEvent -= HandleAddedEvent;
                    };

                    void HandleAddedEvent () => Interop.JS_Export_Custom_BroadcastAddedEvent_Serialized(_id);
                });
            """);
    }

    [Fact]
    public void GeneratesForBuiltInSpecializations ()
    {
        AddAssembly(WithClass(
            """
            [Export] public static ICollection<int> Foo (ICollection<int> col) => default!;
            [Export] public static IList<int> Baz (IList<int> list) => default!;
            [Export] public static IDictionary<int, string> Dic (IDictionary<int, string> dic) => default!;
            [Export] public static CancellationToken Bar (CancellationToken ct) => default!;
            """));
        Execute();
        Contains("int Export (global::System.Collections.Generic.ICollection<global::System.Int32> it) => Export(new global::Bootsharp.Specialized.CollectionExport<global::System.Int32>(it)");
        Contains("int Export (global::System.Collections.Generic.IList<global::System.Int32> it) => Export(new global::Bootsharp.Specialized.ListExport<global::System.Int32>(it)");
        Contains("int Export (global::System.Collections.Generic.IDictionary<global::System.Int32, global::System.String> it) => Export(new global::Bootsharp.Specialized.DictionaryExport<global::System.Int32, global::System.String>(it)");
        Contains("int Export (global::System.Threading.CancellationToken it) => Export(new global::Bootsharp.Specialized.CancellationTokenExport(it)");
        Contains("class JS_Import_System_Collections_Generic_ICollection_Of_System_Int32 (int id) : global::Bootsharp.Specialized.CollectionImport<global::System.Int32>");
        Contains("class JS_Import_System_Collections_Generic_IList_Of_System_Int32 (int id) : global::Bootsharp.Specialized.ListImport<global::System.Int32>");
        Contains("class JS_Import_System_Collections_Generic_IDictionary_Of_System_Int32_And_System_String (int id) : global::Bootsharp.Specialized.DictionaryImport<global::System.Int32, global::System.String>");
        Contains("class JS_Import_System_Threading_CancellationToken (int id) : global::Bootsharp.Specialized.CancellationTokenImport(id)");
    }

    [Fact]
    public void IgnoresTopLevelNullity ()
    {
        // Top-level nullity (IFoo? vs IFoo, IFoo<T>? vs IFoo<T>) must not split the generated proxies,
        // because it does not affect the marshalling/interop shape of the surface members.
        AddAssembly(With(
            """
            public interface IFoo;
            public interface IBar<T>;

            public class Class
            {
                [Import] public static IFoo GetFoo () => default!;
                [Import] public static IFoo? GetFooNullable () => default!;
                [Import] public static IBar<string> GetBar () => default!;
                [Import] public static IBar<string>? GetBarNullable () => default!;
            }
            """));
        Execute();
        Once("class JS_Import_IFoo ");
        Once("class JS_Import_IBar_Of_System_String ");
        DoesNotContain("class JS_Import_IFooOrNull ");
        DoesNotContain("class JS_Import_IBar_Of_System_StringOrNull ");
    }

    [Fact]
    public void DiscriminatesGenericArgNullity ()
    {
        // Generic-arg nullity (IBar<T> vs IBar<T?>) however does affect surface member signatures,
        // so distinct proxies must be generated per nullity variant.
        AddAssembly(With(
            """
            public interface IBar<T>;

            public class Class
            {
                [Import] public static IBar<string> GetBar () => default!;
                [Import] public static IBar<string?> GetBarNullableArg () => default!;
            }
            """));
        Execute();
        Once("class JS_Import_IBar_Of_System_String ");
        Once("class JS_Import_IBar_Of_System_StringOrNull ");
    }

    [Fact]
    public void ReclassifiesImportedNonInterfaceAsExports ()
    {
        // It's impossible to import a concrete C# type, so it's either a user error in the authored interop
        // surface or the intention is to pass back previously exported instance — we assume the latter in the
        // implementation and reclassify to export direction in such cases.
        AddAssembly(With(
            """
            public class Exported;

            public class Class
            {
                [Export] // the idea is that user may pass the result of a previous CreateExported(null) call
                public static Exported CreateExported (Func<Exported> factory = null)
                {
                    return factory?.Invoke() ?? new Exported();
                }
            }
            """));
        Execute();
        DoesNotContain("JS_Import_Exported");
        DoesNotContain("RegisterImport(typeof(global::Exported)");
    }

    [Fact]
    public void DoesNotReclassifySpecializedNonInterfaceImports ()
    {
        // Any specialized type is expected to have hand-rolled exporter and importer proxies,
        // so they're safe to import even when the type is not an interface.
        AddAssembly(WithClass("[Export] public static void Foo (CancellationToken ct) {}"));
        Execute();
        Contains("Instances.RegisterImport(typeof(global::System.Threading.CancellationToken),");
    }
}
