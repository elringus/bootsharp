import { exports } from "./exports.mjs";

const exportedFinalizer = new FinalizationRegistry(finalizeExported);
const exportedById = new Map<number, WeakRef<object>>();
const importedById = new Map<number, object>();
const idByImported = new Map<object, number>();
const onDisposeById = new Map<number, () => void>();
const idPool = new Array<number>();
let nextId = 0; // JS IDs are always positive; C#'s — negative.

export const instances = {
    /** Resolves a registered instance associated with the specified ID,
     *  or uses the specified factory to register a new exported instance. */
    resolve<T extends object>(id: number, factory: new (id: number) => T): T {
        if (id >= 0) return importedById.get(id) as T;
        const exported = exportedById.get(id)?.deref() as T;
        if (exported != null) return exported;
        const proxy = new factory(id);
        exportedById.set(id, new WeakRef(proxy));
        exportedFinalizer.register(proxy, id);
        return proxy;
    },
    /** Registers specified imported (JS) instance and returns the associated unique ID.
     *  Short-circuits already registered imported and exported instances. */
    import(instance: object, cb?: (id: number) => () => void): number {
        const exportedId = (instance as { _id: number })?._id;
        if (exportedId !== undefined) return exportedId;
        const importedId = idByImported.get(instance);
        if (importedId !== undefined) return importedId;
        const id = idPool.length > 0 ? idPool.pop()! : nextId++;
        importedById.set(id, instance);
        idByImported.set(instance, id);
        if (cb != null) onDisposeById.set(id, cb(id));
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
    (exports as { _Bootsharp_Instances_DisposeExported: (id: number) => void })._Bootsharp_Instances_DisposeExported(id);
}
/* v8 ignore stop -- @preserve */
