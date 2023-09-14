import * as bindings from "./bindings.g";
import type { RuntimeAPI } from "./dotnet.d.ts";

export async function bindExports(runtime: RuntimeAPI) {
    const exports = await runtime.getAssemblyExports("Bootsharp");
    (<{ invokable: unknown }><unknown>bindings).invokable =
        (<{ Bootsharp: unknown }>exports).Bootsharp;
}
