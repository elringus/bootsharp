import { RuntimeConfig, RuntimeAPI, module, builder } from "./external";
import { buildConfig } from "./resources";
import { bindImports } from "./imports";
import { bindExports } from "./exports";

/** Lifecycle status of the runtime module. */
export enum BootStatus {
    /** Ready to boot. */
    Standby,
    /** Async boot process is in progress. */
    Booting,
    /** Booted and ready for interop. */
    Booted
}

/** Allows customizing boot process. */
export type BootCustom = {
    /** Customize .NET runtime configuration. */
    config?: RuntimeConfig;
    /** Customize .NET runtime factoring. */
    create?: (config: RuntimeConfig) => Promise<RuntimeAPI>;
    /** Customize binding imported C# APIs. */
    import?: (runtime: RuntimeAPI) => Promise<void>;
    /** Customize .NET runtime startup. */
    run?: (runtime: RuntimeAPI) => Promise<void>;
    /** Customize binding exported C# APIs. */
    export?: (runtime: RuntimeAPI) => Promise<void>;
}

let status = BootStatus.Standby;

/** Returns current runtime module lifecycle state. */
export function getStatus(): BootStatus {
    return status;
}

/** Initializes .NET runtime and binds C# APIs.
 *  @param custom Specify to customize the boot process.
 *  @return Promise that resolves into .NET runtime instance. */
export async function boot(custom?: BootCustom): Promise<RuntimeAPI> {
    status = BootStatus.Booting;
    const config = custom?.config ?? buildConfig();
    const runtime = await custom?.create?.(config) || await builder.withConfig(config).create();
    await custom?.import?.(runtime) || bindImports(runtime);
    await custom?.run?.(runtime) || await runtime.runMain(config.mainAssemblyName!, []);
    await custom?.export?.(runtime) || await bindExports(runtime, config.mainAssemblyName!);
    status = BootStatus.Booted;
    return runtime;
}

/** Terminates .NET runtime and removes WASM module from memory.
 *  @param code Exit code; will use 0 (normal exit) by default.
 *  @param reason Exit reason description (optional). */
export function exit(code?: number, reason?: string): void {
    module.exit(code ?? 0, reason);
    status = BootStatus.Standby;
}
