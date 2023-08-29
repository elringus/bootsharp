import { RuntimeConfig, RuntimeAPI, dotnet, builder } from "./dotnet-api";
import { BootResources, buildConfig } from "./resources";
import { bindImports } from "./imports";
import { bindExports } from "./exports";

/** Allows customizing boot process. */
export type BootCustom = {
    /** Customize .NET runtime configuration. */
    config?: RuntimeConfig;
    /** Customize .NET runtime factoring. */
    create?: (config: RuntimeConfig) => Promise<RuntimeAPI>;
    /** Customize binding imported C# APIs. */
    import?: (runtime: RuntimeAPI) => Promise<void>;
    /** Customize binding exported C# APIs. */
    export?: (runtime: RuntimeAPI) => Promise<void>;
}

/** Initializes .NET runtime and binds C# APIs.
 *  @param resources
 *  Use specified WASM module and assembly binaries to initialize the runtime.
 *  Required when <code>BootsharpEmbedBinaries</code> C# build option is disabled.
 *  @param custom
 *  Specify to customize the boot process.
 *  @return
 *  Promise that resolves into .NET runtime instance. */
export async function boot(resources?: BootResources, custom?: BootCustom): Promise<RuntimeAPI> {
    const config = custom?.config ?? buildConfig(resources);
    const runtime = await custom?.create?.(config) || await builder.withConfig(config).create();
    await custom?.import?.(runtime) || bindImports(runtime);
    await builder.run();
    await custom?.export?.(runtime) || await bindExports(runtime);
    return runtime;
}

/** Terminates .NET runtime and removes WASM module from memory.
 *  @param code
 *  Exit code; will use 0 (normal exit) by default.
 *  @param reason
 *  Exit reason description (optional). */
export function exit(code?: number, reason?: string): void {
    dotnet.exit(code ?? 0, reason);
}
