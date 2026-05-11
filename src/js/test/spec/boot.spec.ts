import { resolve } from "node:path";
import { readFileSync } from "node:fs";
import { describe, expect, it, vi } from "vitest";
import type { BootOptions } from "../cs/Test/bin/bootsharp/index.mjs";

async function setup() {
    vi.resetModules();
    const cs = await import("../cs");
    const test = await import("../cs/Test/bin/bootsharp/generated/test.g.mjs");
    test.Program.onMainInvoked = vi.fn();
    return { ...cs, Program: test.Program };
}

describe("boot", () => {
    it("is standby by default", async () => {
        const { bootsharp } = await setup();
        expect(bootsharp.getStatus()).toStrictEqual(bootsharp.BootStatus.Standby);
    });
    it("throws when exit invoked while not booted", async () => {
        const { bootsharp } = await setup();
        await expect(bootsharp.exit).rejects.toThrow(/not booted/);
    });
    it("transitions to booting and then to booted", async () => {
        const { bootsharp, resources } = await setup();
        const promise = bootsharp.boot(resources);
        expect(bootsharp.getStatus()).toStrictEqual(bootsharp.BootStatus.Booting);
        await promise;
        expect(bootsharp.getStatus()).toStrictEqual(bootsharp.BootStatus.Booted);
    });
    it("throws when boot invoked while booted", async () => {
        const { bootsharp, resources } = await setup();
        await bootsharp.boot(resources);
        await expect(bootsharp.boot(resources)).rejects.toThrow(/already booted/);
    });
    it("throws when boot invoked while booting", async () => {
        const { bootsharp, resources } = await setup();
        const boot = bootsharp.boot(resources);
        await expect(bootsharp.boot(resources)).rejects.toThrow(/already booting/);
        await boot;
    });
    it("invokes program main on boot", async () => {
        const { bootsharp, resources, Program } = await setup();
        await bootsharp.boot(resources);
        expect(Program.onMainInvoked).toHaveBeenCalledOnce();
    });
    it("enables debugging when debugging resources are present", async () => {
        const { bootsharp, resources } = await setup();
        const config = (await bootsharp.boot(resources)).getConfig();
        expect(config.debugLevel).not.toBeUndefined();
    });
    it("doesn't enable debugging when debug are absent", async () => {
        const { bootsharp, resources } = await setup();
        const config = (await bootsharp.boot({ ...resources, symbols: undefined, pdb: undefined })).getConfig();
        expect(config.debugLevel).toBeUndefined();
    });
    it("uses full globalization mode when full ICU resource is present", async () => {
        const { bootsharp, resources } = await setup();
        const config = (await bootsharp.boot(resources)).getConfig();
        expect(config.globalizationMode).toStrictEqual("all");
    });
    it("uses sharded globalization mode when sharded ICU resource is present", async () => {
        const { bootsharp, resources } = await setup();
        const load = (name: string) => {
            const bytes = readFileSync(resolve(`test/cs/Test/bin/Debug/net10.0/browser-wasm/${name}`));
            return { name, content: bytes.buffer.slice(bytes.byteOffset, bytes.byteOffset + bytes.byteLength) };
        };
        const icu = ["icudt_CJK.dat", "icudt_EFIGS.dat", "icudt_no_CJK.dat"].map(load);
        const config = (await bootsharp.boot({ ...resources, icu })).getConfig();
        expect(config.globalizationMode).toStrictEqual("sharded");
    });
    it("disables globalization when ICU resources are absent", async () => {
        const { bootsharp, resources } = await setup();
        const config = (await bootsharp.boot({ ...resources, icu: undefined })).getConfig();
        expect(config.globalizationMode).toStrictEqual("invariant");
    });
    it("fetches resources when root is specified", async () => {
        const { bootsharp, resources, Program } = await setup();
        const bin = [...resources.assemblies!, ...resources.icu!, ...resources.symbols!, ...resources.pdb!];
        const fetchSpy = vi.fn(url => {
            const name = url.substring(url.lastIndexOf("/") + 1);
            const content = bin.find(r => r.name === name)?.content ?? resources.wasm;
            return Promise.resolve({ arrayBuffer: () => Promise.resolve(content) });
        });
        const fetch = global.fetch;
        global.fetch = <never>fetchSpy;
        try { await bootsharp.boot("/bin"); }
        finally { global.fetch = fetch; }
        expect(Program.onMainInvoked).toHaveBeenCalled();
        expect(fetchSpy).toHaveBeenCalledWith("/bin/dotnet.native.wasm");
        expect(fetchSpy).toHaveBeenCalledWith("/bin/Bootsharp.Common.wasm");
    });
    it("respects boot customs", async () => {
        const { bootsharp, resources, manifest } = await setup();
        const customs: BootOptions = {
            config: {
                mainAssemblyName: manifest.entryAssemblyName,
                resources: {
                    jsModuleRuntime: [{
                        name: "dotnet.runtime.js",
                        resolvedUrl: resolve("test/cs/Test/bin/bootsharp/dotnet/dotnet.runtime.js")
                    }],
                    jsModuleNative: [{
                        name: "dotnet.native.js",
                        resolvedUrl: resolve("test/cs/Test/bin/bootsharp/dotnet/dotnet.native.js")
                    }],
                    wasmNative: [{
                        name: "dotnet.native.wasm",
                        buffer: resources.wasm
                    }],
                    assembly: resources.assemblies?.map(a => ({
                        name: a.name,
                        virtualPath: a.name,
                        buffer: a.content!
                    }))
                }
            },
            create: vi.fn(async () => await bootsharp.dotnet.withConfig(customs.config!).create()),
            import: vi.fn(),
            run: vi.fn(),
            export: vi.fn()
        };
        await bootsharp.boot("does not matter with the custom create hook", customs);
        expect(customs.create).toHaveBeenCalledWith(customs.config);
        expect(customs.import).toHaveBeenCalledOnce();
        expect(customs.run).toHaveBeenCalledOnce();
        expect(customs.export).toHaveBeenCalledOnce();
    });
    it("can boot when program has no exports", async () => {
        const { bootsharp, resources } = await setup();
        const options: BootOptions = {
            create: vi.fn(async cfg => {
                const runtime = await bootsharp.dotnet.withConfig(cfg).create();
                runtime.getAssemblyExports = () => Promise.resolve({
                    Bootsharp: { Generated: { Instances: { DisposeExported: () => {} } } }
                });
                return runtime;
            })
        };
        await bootsharp.boot(resources, options);
    });
});
