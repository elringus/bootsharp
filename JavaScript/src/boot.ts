import { RuntimeAPI } from "./dotnet-types";
import { RuntimeConfig, builder, exit as _exit } from "./dotnet-api";
import { BootResources, resources as _resources, buildConfig } from "./resources";
import { Binding, bindings } from "./bindings";

/** Allows customizing .NET runtime initialization process. */
export type BootCustom = {
    /** Create .NET runtime configuration. */
    config?: (resources: BootResources) => RuntimeConfig;
    /** Create .NET runtime using specified configuration. */
    create?: (config: RuntimeConfig) => Promise<RuntimeAPI>;
    /** Assign imported C# bindings. */
    bind?: (runtime: RuntimeAPI, bindings: Binding[]) => void;
}

/**
 * Initializes .NET runtime.
 * @param resources
 * When specified, will use the resources to boot the runtime.
 * Required when <code>BootsharpEmbedBinaries</code> C# build option is disabled.
 * @param custom
 * Use to customize .NET runtime initialization process.
 * @return
 * Promise that resolves into .NET runtime instance.
 */
export async function boot(resources?: BootResources, custom?: BootCustom): Promise<RuntimeAPI> {
    const config = createConfig(resources ?? _resources, custom);
    const runtime = await createRuntime(config, custom);
    bind(runtime, custom);
    await builder.run();
    return runtime;
}

/**
 * Terminates .NET runtime and removes WASM module from memory.
 * @param code
 * Exit code; will use 0 (normal exit) by default.
 * @param reason
 * Exit reason description (optional).
 */
export function exit(code?: number, reason?: string): void {
    _exit(code ?? 0, reason);
}

function createConfig(res: BootResources, custom?: BootCustom): RuntimeConfig {
    if (custom?.config != null) return custom.config(res);
    return buildConfig(res);
}

function createRuntime(config: RuntimeConfig, custom?: BootCustom): Promise<RuntimeAPI> {
    if (custom?.create != null) return custom.create(config);
    return builder.withConfig(config).create();
}

function bind(runtime: RuntimeAPI, custom?: BootCustom): void {
    if (custom?.bind != null) custom.bind(runtime, bindings);
    // ...
}
