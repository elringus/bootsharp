import * as bindings from "./bindings.g";
import { instances } from "./instances";
import type { RuntimeAPI } from "./modules";

export function bindImports(runtime: RuntimeAPI) {
    runtime.setModuleImports("Bootsharp", {
        ...bindings,
        instances
    });
}
