import * as bindings from "./bindings.g";
import { disposeImported } from "./instances";
import type { RuntimeAPI } from "./modules";

export function bindImports(runtime: RuntimeAPI) {
    runtime.setModuleImports("Bootsharp", {
        ...bindings,
        disposeImported
    });
}
