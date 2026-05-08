import { RuntimeConfig, RuntimeAPI, app } from "./dotnet/index.mjs";
import { BootResources, fetchResources } from "./resources.mjs";
import { buildConfig } from "./config.mjs";
import { bindImports } from "./imports.mjs";
import { bindExports } from "./exports.mjs";
import { setRuntime } from "./runtime.mjs";

/** Lifecycle status of the runtime module. */
export enum BootStatus {
    /** Ready to boot. */
    Standby,
    /** Async boot process is in progress. */
    Booting,
    /** Booted and ready for interop. */
    Booted
}

/** Configuration of the runtime boot process. */
export type BootOptions = {
    /** Resources required to boot the runtime. */
    readonly resources: BootResources;
    /** Custom runtime configuration. */
    readonly config?: RuntimeConfig;
    /** Customization hook for creating the runtime instance. */
    readonly create?: (config: RuntimeConfig) => Promise<RuntimeAPI>;
    /** Customization hook for binding imported C# APIs. */
    readonly import?: (runtime: RuntimeAPI) => Promise<void>;
    /** Customization hook for binding exported C# APIs. */
    readonly export?: (runtime: RuntimeAPI) => Promise<void>;
    /** Customization hook for starting the runtime. */
    readonly run?: (runtime: RuntimeAPI) => Promise<void>;
}

let status = BootStatus.Standby;

/** Returns current runtime module lifecycle state. */
export function getStatus(): BootStatus {
    return status;
}

/** Initializes the runtime and binds C# APIs.
 *  @param opt Either URL to the boot resources root (eg, <code>/bin</code>) or a full configuration.
 *  @return Promise that resolves into the runtime instance when the initialization is finished. */
export async function boot(opt: string | BootOptions): Promise<RuntimeAPI> {
    if (status === BootStatus.Booted) throw Error("Failed to boot the C# runtime: already booted.");
    if (status === BootStatus.Booting) throw Error("Failed to boot the C# runtime: already booting.");
    status = BootStatus.Booting;
    const options = typeof opt === "string" ? { resources: await fetchResources(opt) } : opt;
    const runtime = await createRuntime(options);
    status = BootStatus.Booted;
    return runtime;
}

/** Terminates the runtime and removes WASM module from memory.
 *  @param code Exit code; will use 0 (normal exit) by default.
 *  @param reason Exit reason description (optional). */
export async function exit(code?: number, reason?: string): Promise<void> {
    /* v8 ignore start -- @preserve */ // Uncoverable, as exit terminates the host test process.
    if (status !== BootStatus.Booted) throw Error("Failed to exit the C# runtime: not booted.");
    try { app.exit(code ?? 0, reason); }
    catch { }
    finally { status = BootStatus.Standby; }
    /* v8 ignore stop -- @preserve */
}

async function createRuntime(opt: BootOptions) {
    const cfg = opt.config ?? buildConfig(opt.resources);
    const runtime = await opt.create?.(cfg) || await app.dotnet.withConfig(cfg).create();
    setRuntime(runtime);
    if (opt.import) await opt.import(runtime); else bindImports(runtime);
    if (opt.run) await opt.run(runtime); else await runtime.runMain(cfg.mainAssemblyName!, []);
    if (opt.export) await opt.export(runtime); else await bindExports(runtime, cfg.mainAssemblyName!);
    return runtime;
}
