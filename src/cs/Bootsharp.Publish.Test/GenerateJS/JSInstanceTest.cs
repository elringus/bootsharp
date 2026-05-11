namespace Bootsharp.Publish.Test;

public class JSInstanceTest : GenerateJSTest
{
    protected override string TestedContent { get => field ?? ReadProjectFile("generated/instances.g.mjs") ?? ""; set; }

    [Fact]
    public void GeneratesForMethods ()
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
        Contains(
            """
            $i.IExported = class JSExported {
                constructor(_id) { this._id = _id; }
                inv(it, info) { return index.IExported.inv(this._id, it, info); }
            };
            """);
        Contains("index.g.mjs",
            """
            export const Class = {
                getExported: async (it) => $i.resolve(await exports.Class_GetExported($i.import(it)), $i.IExported),
                get getImported() { return this.getImportedHandler; },
                set getImported(handler) { this.getImportedHandler = handler; this.getImportedSerializedHandler = async (it) => $i.import(await this.getImportedHandler($i.resolve(it, $i.IExported))); },
                get getImportedSerialized() { return this.getImportedSerializedHandler; }
            };
            export const IImported = {
                funSerialized: (_id, it, info) => serialize($i.imported(_id).fun($i.resolve(it, $i.IImported), deserialize(info, $s.Info)), $s.Info),
                fun: (_id, it, info) => deserialize(exports.Bootsharp_Generated_Exports_JSImported_Fun(_id, $i.import(it), serialize(info, $s.Info)), $s.Info)
            };
            export const IExported = {
                inv: (_id, it, info) => deserialize(exports.Bootsharp_Generated_Exports_JSExported_Inv(_id, $i.import(it), serialize(info, $s.Info)), $s.Info),
                invSerialized: (_id, it, info) => serialize($i.imported(_id).inv($i.resolve(it, $i.IExported), deserialize(info, $s.Info)), $s.Info)
            };
            """);
    }

    [Fact]
    public void GeneratesForProperties ()
    {
        AddAssembly(With(
            """
            public record Info (string Value);

            public interface IExported
            {
                Info? State { get; set; }
                IExported Exported { get; }
                IImported Imported { set; }
            }

            public interface IImported
            {
                Info? State { get; set; }
                IImported Imported { get; }
                IExported Exported { set; }
            }

            public partial class Class
            {
                [Export] public static IExported GetExported (IImported it) => default;
                [Import] public static IImported GetImported (IExported it) => default;
            }
            """));
        Execute();
        Contains(
            """
            $i.IExported = class JSExported {
                constructor(_id) { this._id = _id; }
                get state() { return index.IExported.getState(this._id); }
                set state(value) { index.IExported.setState(this._id, value); }
                get exported() { return index.IExported.getExported(this._id); }
                set imported(value) { index.IExported.setImported(this._id, value); }
            };
            """);
        Contains("index.g.mjs",
            """
            export const Class = {
                getExported: (it) => $i.resolve(exports.Class_GetExported($i.import(it)), $i.IExported),
                get getImported() { return this.getImportedHandler; },
                set getImported(handler) { this.getImportedHandler = handler; this.getImportedSerializedHandler = (it) => $i.import(this.getImportedHandler($i.resolve(it, $i.IExported))); },
                get getImportedSerialized() { return this.getImportedSerializedHandler; }
            };
            export const IImported = {
                getStateSerialized(_id) { return serialize($i.imported(_id).state, $s.Info); },
                setStateSerialized(_id, value) { $i.imported(_id).state = deserialize(value, $s.Info); },
                getImportedSerialized(_id) { return $i.import($i.imported(_id).imported); },
                setExportedSerialized(_id, value) { $i.imported(_id).exported = $i.resolve(value, $i.IExported); }
            };
            export const IExported = {
                getState(_id) { return deserialize(exports.Bootsharp_Generated_Exports_JSExported_GetState(_id), $s.Info) ?? undefined; },
                setState(_id, value) { exports.Bootsharp_Generated_Exports_JSExported_SetState(_id, serialize(value, $s.Info)); },
                getExported(_id) { return $i.resolve(exports.Bootsharp_Generated_Exports_JSExported_GetExported(_id), $i.IExported); },
                setImported(_id, value) { exports.Bootsharp_Generated_Exports_JSExported_SetImported(_id, $i.import(value)); }
            };
            """);
    }

    [Fact]
    public void GeneratesForEvents ()
    {
        AddAssembly(With(
            """
            public record Info (string Value);

            public interface IExported { event Action<IExported, Info>? Changed; }
            public interface IImported { event Action<IImported, Info>? Changed; }

            public partial class Class
            {
                [Export] public static IExported GetExported (IImported it) => default;
                [Import] public static IImported GetImported (IExported it) => default;
            }
            """));
        Execute();
        Contains(
            """
            $i.IExported = class JSExported {
                constructor(_id) { this._id = _id; }
                changed = new Event();
                broadcastChanged(arg1, arg2) { this.changed.broadcast(arg1, arg2); }
            };
            """);
        Contains(
            """
            $i.import_IImported = function (it) {
                return $i.import(it, _id => {
                    it.changed.subscribe(handleChanged);
                    return () => {
                        it.changed.unsubscribe(handleChanged);
                    };

                    function handleChanged(arg1, arg2) { exports.Bootsharp_Generated_Imports_JSImported_InvokeChanged(_id, $i.import_IImported(arg1), serialize(arg2, $s.Info)); }
                });
            };
            """);
        Contains("index.g.mjs",
            """
            export const Class = {
                getExported: (it) => $i.resolve(exports.Class_GetExported($i.import_IImported(it)), $i.IExported),
                get getImported() { return this.getImportedHandler; },
                set getImported(handler) { this.getImportedHandler = handler; this.getImportedSerializedHandler = (it) => $i.import_IImported(this.getImportedHandler($i.resolve(it, $i.IExported))); },
                get getImportedSerialized() { return this.getImportedSerializedHandler; }
            };
            export const IImported = {
            };
            export const IExported = {
                broadcastChangedSerialized: (_id, arg1, arg2) => $i.resolve(_id, $i.IExported).broadcastChanged($i.resolve(arg1, $i.IExported), deserialize(arg2, $s.Info))
            };
            """);
    }

    [Fact]
    public void DoesNotEmitDuplicateSpecializedImporters ()
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
        Once(@"\$i\.import_IBi = function");
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
    public void ImportersDontLeakToModule ()
    {
        AddAssembly(With(
            """
            public interface IExportedInstanced { void Inv (); }
            public interface IImportedInstanced { event Action? OnChanged; }
            public class Class
            {
                [Export] public static IExportedInstanced GetExported () => default!;
                [Import] public static IImportedInstanced GetImported () => default!;
            }
            """));
        Execute();
        Contains("$i.IExportedInstanced = class JSExportedInstanced");
        Contains("$i.import_IImportedInstanced = function");
        DoesNotContain("index.g.mjs", "$i.IExportedInstanced = class");
        DoesNotContain("index.g.mjs", "$i.import_IImportedInstanced = function");
    }

    [Fact]
    public void CanReferenceObjectsFromOtherModules ()
    {
        AddAssembly(With(
            """
            namespace Foo.Bar;

            public interface IExported
            {
                int State { get; }
                void Method ();
            }

            public partial class Class
            {
                [Export] public static IExported Get () => default;
            }
            """));
        Execute();
        Contains(
            """
            import * as foo_bar from "./foo/bar.g.mjs";

            $i.Foo_Bar_IExported = class Foo_Bar_JSExported {
                constructor(_id) { this._id = _id; }
                get state() { return foo_bar.IExported.getState(this._id); }
                method() { foo_bar.IExported.method(this._id); }
            };
            """);
    }
}
