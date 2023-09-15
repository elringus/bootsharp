import * as bindings from "./bindings.g";
import type { RuntimeAPI } from "./dotnet.d.ts";

export function bindImports(runtime: RuntimeAPI) {
    runtime.setModuleImports("Bootsharp", bindings);
}
