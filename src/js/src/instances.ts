import { exports } from "./exports";

const finalizer = new FinalizationRegistry(finalizeInstance);
const idToInstance = new Map<number, object>();
const idPool = new Array<number>();
let nextId = Number.MIN_SAFE_INTEGER;

/** Registers specified imported (JS -> C#) interop instance and associates it with unique ID.
 *  @param instance Interop instance to resolve ID for.
 *  @return Unique identifier of the registered instance. */
export function registerInstance(instance: object): number {
    const id = idPool.length > 0 ? idPool.shift()! : nextId++;
    idToInstance.set(id, instance);
    return id;
}

/** Resolves registered imported (JS -> C#) interop instance from specified ID.
 *  @param id Unique identifier of the instance. */
export function getInstance(id: number): object {
    return idToInstance.get(id)!;
}

/** Invoked from C# to notify that imported (JS -> C#) interop instance is no longer
 *  used (eg, was garbage collected) and can be released on JavaScript side as well.
 *  @param id Unique identifier of the disposed interop instance. */
export function disposeInstance(id: number): void {
    idToInstance.delete(id);
    idPool.push(id);
}

/** Registers specified exported (C# -> JS) instance to invoke dispose on C# side
 *  when it's collected (finalized) by JavaScript runtime GC.
 *  @param instance Interop instance to register.
 *  @param id Unique identifier of the interop instance. */
export function disposeOnFinalize(instance: object, id: number): void {
    finalizer.register(instance, id);
}

function finalizeInstance(id: number) {
    (<{ DisposeExportedInstance: (id: number) => void }>exports).DisposeExportedInstance(id);
}
