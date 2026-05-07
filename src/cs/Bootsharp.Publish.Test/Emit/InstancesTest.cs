namespace Bootsharp.Publish.Test;

public class InstancesTest : EmitTest
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
            namespace Bootsharp.Generated.Imports
            {
                public class JSImported (global::System.Int32 id) : global::IImported
                {
                    internal readonly global::System.Int32 _id = id;

                    ~JSImported() => Instances.DisposeImported(_id);

                    public event global::System.Action<global::Record?> OnRecordChanged;
                    internal void InvokeOnRecordChanged (global::Record? obj) => OnRecordChanged?.Invoke(obj);
                    public event global::IImported.SomethingChanged OnSomethingChanged;
                    internal void InvokeOnSomethingChanged () => OnSomethingChanged?.Invoke();
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
                    internal static int Export (global::IExported instance) => Export(instance, static (_id, instance) => {
                        instance.Changed += HandleChanged;
                        return () => {
                            instance.Changed -= HandleChanged;
                        };

                        void HandleChanged (global::Record arg1, global::IExported arg2) => Interop.Exported_BroadcastChanged_Serialized(_id, Serializer.Serialize(arg1, SerializerContext.Record), Instances.Export(arg2));
                    });
            """);
    }

    [Fact]
    public void DoesNotGenerateDuplicateSpecializedExports ()
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
        Once(@"internal static int Export \(global::IExported instance\)");
    }
}
