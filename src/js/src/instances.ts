import { exports } from "./exports";

export function disposeImportedInstance(id: number) {

}

function disposeExportedInstance(id: number) {
    (<{ DisposeExportedInstance: (id: number) => void }>exports).DisposeExportedInstance(id);
}
