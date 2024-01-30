import { exports } from "./exports";

/** Resolves registered imported (JS -> C#) interop instance from specified ID.
 *  @param id Unique identifier of the instance. */
export function getInstance(id: number): object {
    return {};
}

/** Resolves unique ID of the specified imported (JS -> C#) interop instance.
 *  @param instance Interop instance to resolve ID for. */
export function getInstanceId(instance: object): number {
    // if exist return, otherwise create and
    // register with FinalizationRegistry here to call disposeExportedInstance
    return -1;
}

export function disposeImportedInstance(id: number): void {

}

function disposeExportedInstance(id: number): void {
    (<{ DisposeExportedInstance: (id: number) => void }>exports).DisposeExportedInstance(id);
}
