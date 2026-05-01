import { exports } from "./exports";

const exportedFinalizer = new FinalizationRegistry(finalizeExported);
const exportedById = new Map<number, WeakRef<object>>();
const importedById = new Map<number, object>();
const idByImported = new Map<object, number>();
const onDisposeById = new Map<number, () => void>();
const idPool = new Array<number>();
let nextId = -2147483648; // Number.MIN_SAFE_INTEGER is below C#'s Int32.MinValue

export const instances = {
    /** Invokes the specified factory to create and register an exported instance wrapper associated with the ID,
     *  unless an exported instance is already registered under the ID, in which case returns its wrapper. */
    export(id: number, factory: (id: number) => object): object {
        const instance = exportedById.get(id)?.deref();
        if (instance != null) return instance;
        const exported = factory(id);
        exportedById.set(id, new WeakRef(exported));
        exportedFinalizer.register(exported, id);
        return exported;
    },
    /** Registers specified imported instance and associates it with a unique ID, unless it's already registered,
     *  in which case the ID of the registered instance is returned. */
    import(instance: object, factory?: (id: number) => () => void): number {
        const registered = idByImported.get(instance);
        if (registered !== undefined) return registered;
        const id = idPool.length > 0 ? idPool.pop()! : nextId++;
        importedById.set(id, instance);
        idByImported.set(instance, id);
        if (factory != null) onDisposeById.set(id, factory(id));
        return id;
    },
    /** Returns a registered imported instance associated with the specified ID. */
    imported(id: number): object {
        return importedById.get(id)!;
    },
    /** Invoked from C# to notify that the imported (JS -> C#) instance is no longer used
     *  (eg, was garbage collected) and can be released on the JavaScript side as well.
     *  @param id Unique identifier of the disposed instance. */
    disposeImported(id: number): void {
        idByImported.delete(importedById.get(id)!);
        importedById.delete(id);
        onDisposeById.get(id)?.();
        onDisposeById.delete(id);
        idPool.push(id);
    }
};

/* v8 ignore start -- @preserve */ // Uncoverable, as finalization in Node is not controllable.
function finalizeExported(id: number) {
    exportedById.delete(id);
    (<{ DisposeExportedInstance: (id: number) => void }>exports).DisposeExportedInstance(id);
}
/* v8 ignore stop -- @preserve */
