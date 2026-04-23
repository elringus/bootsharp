import { exports } from "./exports";

const exportedFinalizer = new FinalizationRegistry(finalizeExported);
const exportedById = new Map<number, WeakRef<object>>();
const importedById = new Map<number, object>();
const idByImported = new Map<object, number>();
const onDisposeById = new Map<number, () => void>();
const idPool = new Array<number>();
let nextId = -2147483648; // Number.MIN_SAFE_INTEGER is below C#'s Int32.MinValue

/** Registers specified imported (JS -> C#) interop instance and associates it with unique ID.
 *  @param instance Interop instance to resolve ID for.
 *  @param onDispose Invoked when disposing the registered instance.
 *  @return Unique identifier of the registered instance. */
export function registerImported(instance: object, onDispose?: () => void): number {
    const registered = idByImported.get(instance);
    if (registered !== undefined) return registered;
    const id = idPool.length > 0 ? idPool.pop()! : nextId++;
    importedById.set(id, instance);
    idByImported.set(instance, id);
    if (onDispose != null) onDisposeById.set(id, onDispose);
    return id;
}

/** Registers specified exported (C# -> JS) interop instance with the specified ID. */
export function registerExported(instance: object, id: number): void {
    exportedById.set(id, new WeakRef(instance));
    exportedFinalizer.register(instance, id);
}

/** Returns currently registered imported instance with the specified ID. */
export function getImported(id: number): object {
    return importedById.get(id)!;
}

/** Resolves ID of the specified registered imported instance; returns undefined when not registered. */
export function getImportedId(instance: object): number | undefined {
    return idByImported.get(instance);
}

/** Returns currently registered exported instance with the specified ID. */
export function getExported(id: number): object {
    return exportedById.get(id)!.deref()!;
}

/** Invoked from C# to notify that the imported (JS -> C#) interop instance is no longer
 *  used (eg, was garbage collected) and can be released on the JavaScript side as well.
 *  @param id Unique identifier of the disposed interop instance. */
export function disposeImported(id: number): void {
    idByImported.delete(importedById.get(id)!);
    importedById.delete(id);
    onDisposeById.get(id)?.();
    onDisposeById.delete(id);
    idPool.push(id);
}

/* v8 ignore start -- @preserve */ // Uncoverable, as finalization in Node is not controllable.
function finalizeExported(id: number) {
    exportedById.delete(id);
    (<{ DisposeExportedInstance: (id: number) => void }>exports).DisposeExportedInstance(id);
}
/* v8 ignore stop -- @preserve */
