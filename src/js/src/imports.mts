import * as generated from "./generated/bindings.g.mjs";
import { instances } from "./instances.mjs";
import type { RuntimeAPI } from "./dotnet/index.mjs";

export function bindImports(runtime: RuntimeAPI) {
    runtime.setModuleImports("Bootsharp", {
        ...generated,
        instances
    });
}
