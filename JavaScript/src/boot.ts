import { Base64 } from "js-base64";
import { RuntimeConfig, dotnet, exit as _exit } from "./dotnet-api";
import { BootResources, resources as _resources, validateResources } from "./resources";
import { Binding, bindings } from "./bindings";
import { RuntimeAPI } from "./dotnet-types";

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
 * Assign to customize .NET runtime initialization process.
 * @return
 * Promise that resolves into .NET runtime instance.
 */
export async function boot(resources?: BootResources, custom?: BootCustom): Promise<RuntimeAPI> {
    const res = validateResources(resources ?? _resources);
    const config = createConfig(res, custom);
    const runtime = await createRuntime(config, custom);
    bind(runtime, custom);
    await dotnet.run();
    return runtime;
}

/**
 * Terminates .NET runtime and removes the module from memory.
 * @param code
 * Exit code; will use 0 (normal exit) by default.
 * @param reason
 * Exit reason description (optional).
 */
export function exit(code?: number, reason?: any) {
    _exit(code ?? 0, reason);
}

function createConfig(res: BootResources, custom?: BootCustom): RuntimeConfig {
    if (custom?.config != null) return custom.config(res);
    return null!;
}

function createRuntime(config: RuntimeConfig, custom?: BootCustom): Promise<RuntimeAPI> {
    if (custom?.create != null) return custom.create(config);
    return dotnet.withConfig(config).create();
}

function bind(runtime: RuntimeAPI, custom?: BootCustom) {
    if (custom?.bind != null) custom.bind(runtime, bindings);
    // ...
}

function toBinary(data: Uint8Array | string) {
    return typeof data === "string" ? Base64.toUint8Array(data) : data;
}
