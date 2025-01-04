import { RuntimeConfig, RuntimeAPI, getMain, ModuleAPI } from "./modules";
import { BootResources, resources } from "./resources";
import { buildConfig } from "./config";
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

/** Boot process configuration. */
export type BootOptions = {
    /** Absolute path to the directory where boot resources are hosted (eg, <code>/bin</code>). */
    readonly root?: string;
    /** Resources required to boot .NET runtime. */
    readonly resources?: BootResources;
    /** .NET runtime configuration. */
    readonly config?: RuntimeConfig;
    /** Creates .NET runtime instance. */
    readonly create?: (config: RuntimeConfig) => Promise<RuntimeAPI>;
    /** Binds imported C# APIs. */
    readonly import?: (runtime: RuntimeAPI) => Promise<void>;
    /** Starts .NET runtime. */
    readonly run?: (runtime: RuntimeAPI) => Promise<void>;
    /** Binds exported C# APIs. */
    readonly export?: (runtime: RuntimeAPI) => Promise<void>;
}

let status = BootStatus.Standby;
let main: ModuleAPI | undefined;

/** Returns current runtime module lifecycle state. */
export function getStatus(): BootStatus {
    return status;
}

/** Initializes .NET runtime and binds C# APIs.
 *  @param options Specify to configure the boot process.
 *  @return Promise that resolves into .NET runtime instance. */
export async function boot(options?: BootOptions): Promise<RuntimeAPI> {
    if (status === BootStatus.Booted) throw Error("Failed to boot .NET runtime: already booted.");
    if (status === BootStatus.Booting) throw Error("Failed to boot .NET runtime: already booting.");
    status = BootStatus.Booting;
    main = await getMain(options?.root);
    const runtime = await createRuntime(main, options);
    status = BootStatus.Booted;
    return runtime;
}

/** Terminates .NET runtime and removes WASM module from memory.
 *  @param code Exit code; will use 0 (normal exit) by default.
 *  @param reason Exit reason description (optional). */
export async function exit(code?: number, reason?: string): Promise<void> {
    if (status !== BootStatus.Booted) throw Error("Failed to exit .NET runtime: not booted.");
    try { main?.exit(code ?? 0, reason); }
    catch { }
    finally { status = BootStatus.Standby; }
}

async function createRuntime(main: ModuleAPI, opt?: BootOptions) {
    const cfg = opt?.config ?? await buildConfig(opt?.resources ?? resources, opt?.root);
    const runtime = await opt?.create?.(cfg) || await main.dotnet.withConfig(cfg).create();
    if (opt?.import) await opt.import(runtime); else bindImports(runtime);
    if (opt?.run) await opt.run(runtime); else await runtime.runMain(cfg.mainAssemblyName!, []);
    if (opt?.export) await opt.export(runtime); else await bindExports(runtime, cfg.mainAssemblyName!);
    return runtime;
}
