import { exports } from "./exports";

/** Registers specified imported (JS -> C#) interop instance and associates it with unique ID.
 *  @param instance Interop instance to resolve ID for.
 *  @return Unique ID of the registered instance. */
export function registerInstance(instance: object): number {

    return -1;
}

/** Resolves registered imported (JS -> C#) interop instance from specified ID.
 *  @param id Unique identifier of the instance. */
export function getInstance(id: number): object {
    return {};
}

/** Invoked from C# to notify that imported (JS -> C#) interop instance is no longer
 *  used (eg, was garbage collected) and can be released on JavaScript side as well.
 *  @param id ID of the disposed interop instance. */
export function disposeImportedInstance(id: number): void {

}

/** Registers specified exported (C# -> JS) instance to invoke dispose on C# side
 *  when it's collected (finalized) by JavaScript runtime GC.
 *  @param instance Interop instance to register. */
export function disposeOnFinalize(instance: object): void {

}

/** Invoked from JavaScript to notify that exported (C# -> JS) interop instance is
 *  no longer used (eg, was garbage collected) and can be released on C# side as well.
 *  @param id ID of the disposed interop instance. */
function disposeExportedInstance(id: number): void {
    (<{ DisposeExportedInstance: (id: number) => void }>exports).DisposeExportedInstance(id);
}
