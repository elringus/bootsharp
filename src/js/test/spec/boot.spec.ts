import { describe, expect, it, vi } from "vitest";
import { resolve } from "node:path";
import { Buffer } from "node:buffer";
import type { BootOptions } from "../cs/Test/bin/sideload";

async function setup() {
    // dotnet merges with the host node process, so it's not possible
    // to exit w/o killing the test process (which is bound to test file);
    // this is a workaround to simulate clean environment in each test
    vi.resetModules();
    const cs = await import("../cs");
    cs.SideloadTest.Program.onMainInvoked = vi.fn();
    cs.EmbeddedTest.Program.onMainInvoked = vi.fn();
    return {
        ...cs,
        side: { bootsharp: cs.sideload, Test: cs.SideloadTest },
        embed: { bootsharp: cs.embedded, Test: cs.EmbeddedTest }
    };
}

describe("boot", () => {
    it("uses embedded modules when root is not specified", async () => {
        const { side: { bootsharp } } = await setup();
        expect((await bootsharp.dotnet.getMain()).embedded).toStrictEqual(false);
        expect((await bootsharp.dotnet.getNative()).embedded).toStrictEqual(false);
        expect((await bootsharp.dotnet.getRuntime()).embedded).toStrictEqual(false);
    });
    it("uses sideload modules when root is specified", async () => {
        const { side: { bootsharp }, root } = await setup();
        expect((await bootsharp.dotnet.getMain(root)).embedded).toBeUndefined();
        expect((await bootsharp.dotnet.getNative(root)).embedded).toBeUndefined();
        expect((await bootsharp.dotnet.getRuntime(root)).embedded).toBeUndefined();
    });
    it("defines module exports when root is not specified", async () => {
        const { embed: { bootsharp } } = await setup();
        const config = await bootsharp.dotnet.buildConfig(bootsharp.resources);
        expect(config.assets!.find(a => a.behavior === "js-module-dotnet")!.moduleExports).toBeDefined();
        expect(config.assets!.find(a => a.behavior === "js-module-native")!.moduleExports).toBeDefined();
        expect(config.assets!.find(a => a.behavior === "js-module-runtime")!.moduleExports).toBeDefined();
    });
    it("throws when missing boot resource", async () => {
        const { side: { bootsharp } } = await setup();
        await expect(bootsharp.dotnet.buildConfig(bootsharp.resources))
            .rejects.toThrowError(/Failed to resolve '.+' boot resource/);
    });
    it("throws when attempting to fetch boot resource in sandbox", async () => {
        const { side: { bootsharp }, root, any } = await setup();
        const win = any(global).window;
        const proc = any(global).process;
        any(global).window = undefined;
        any(global).process = undefined;
        await expect(bootsharp.dotnet.buildConfig(bootsharp.resources, root))
            .rejects.toThrowError(/Failed to fetch '.+' boot resource: unsupported runtime/);
        any(global).window = win;
        any(global).process = proc;
    });
    it("uses fetch when fetching boot resource in browser", async () => {
        const { side: { bootsharp }, root, any, bins } = await setup();
        const win = any(global).window;
        const fetch = global.fetch;
        any(global).window = {};
        any(global).fetch = vi.fn(() => ({ arrayBuffer: () => bins.wasm }));
        await bootsharp.dotnet.buildConfig(bootsharp.resources, root);
        expect(global.fetch).toHaveBeenCalled();
        any(global).window = win;
        any(global).fetch = fetch;
    });
    it("can boot with embedded resources", async () => {
        const { embed: { bootsharp, Test } } = await setup();
        await bootsharp.boot({});
        expect(Test.Program.onMainInvoked).toHaveBeenCalledOnce();
    });
    it("can boot while streaming resources from root", async () => {
        const { side: { bootsharp }, root, Test } = await setup();
        await bootsharp.boot({ root });
        expect(Test.Program.onMainInvoked).toHaveBeenCalledOnce();
    });
    it("can boot with resources content pre-assigned", async () => {
        const { side: { bootsharp }, Test, root, bins, any } = await setup();
        const resources = { ...bootsharp.resources };
        any(resources.wasm).content = bins.wasm;
        for (const asm of resources.assemblies)
            any(asm).content = bins.assemblies.find(a => a.name === asm.name)!.content;
        await bootsharp.boot({ resources, root });
        expect(Test.Program.onMainInvoked).toHaveBeenCalledOnce();
    });
    it("can boot with base64 content", async () => {
        const { side: { bootsharp }, Test, root, bins, any } = await setup();
        const resources = { ...bootsharp.resources };
        any(resources.wasm).content = bins.wasm.toString("base64");
        for (const asm of resources.assemblies)
            any(asm).content = bins.assemblies.find(a => a.name === asm.name)!.content.toString("base64");
        await bootsharp.boot({ resources, root });
        expect(Test.Program.onMainInvoked).toHaveBeenCalledOnce();
    });
    it("uses atob when window is defined in global", async () => {
        const { bins, any } = await setup();
        const win = any(global).window;
        any(global).window = { atob: vi.fn(src => Buffer.from(src, "base64").toString("binary")) };
        // @ts-expect-error
        const { decodeBase64 } = await import("../cs/Test/bin/sideload/decoder.mjs");
        try { decodeBase64(bins.assemblies[0].content.toString("base64")); }
        catch {}
        expect(global.window.atob).toHaveBeenCalled();
        any(global).window = win;
    });
    it("uses naive decoder when neither window nor process are defined", async () => {
        const { bins, any } = await setup();
        const win = any(global).window;
        const proc = any(global).process;
        any(global).window = undefined;
        any(global).process = undefined;
        // @ts-expect-error
        const { decodeBase64 } = await import("../cs/Test/bin/sideload/decoder.mjs");
        for (const ass of bins.assemblies)
            expect(decodeBase64(ass.content.toString("base64")).byteLength)
                .toStrictEqual(ass.content.length);
        any(global).window = win;
        any(global).process = proc;
    });
    it("throws when boot invoked while booted", async () => {
        const { side: { bootsharp }, root } = await setup();
        await bootsharp.boot({ root });
        await expect(bootsharp.boot).rejects.toThrow(/already booted/);
    });
    it("throws when boot invoked while booting", async () => {
        const { side: { bootsharp }, root } = await setup();
        const boot = bootsharp.boot({ root });
        await expect(bootsharp.boot).rejects.toThrow(/already booting/);
        await boot;
    });
    it("throws when exit invoked while not booted", async () => {
        const { side: { bootsharp } } = await setup();
        await expect(bootsharp.exit).rejects.toThrow(/not booted/);
    });
    it("can exit when booted", async () => {
        const { side: { bootsharp }, root } = await setup();
        await bootsharp.boot({ root });
        await bootsharp.exit();
        expect(bootsharp.getStatus()).toStrictEqual(0);
    });
    it("respects boot customs", async () => {
        const { side: { bootsharp }, bins, root } = await setup();
        const customs: BootOptions = {
            config: {
                mainAssemblyName: bins.entryAssemblyName,
                assets: [
                    {
                        name: resolve("test/cs/Test/bin/sideload/bin/dotnet.js"),
                        behavior: "js-module-dotnet"
                    },
                    {
                        name: resolve("test/cs/Test/bin/sideload/bin/dotnet.runtime.js"),
                        behavior: "js-module-runtime"
                    },
                    {
                        name: resolve("test/cs/Test/bin/sideload/bin/dotnet.native.js"),
                        behavior: "js-module-native"
                    },
                    {
                        name: "dotnet.native.wasm",
                        buffer: <never>bins.wasm.buffer,
                        behavior: "dotnetwasm"
                    },
                    ...bins.assemblies.map(a => (<never>{ name: a.name, buffer: a.content, behavior: "assembly" }))
                ]
            },
            create: vi.fn(async () => {
                const bootsharp = (await import("../cs/Test/bin/sideload")).default;
                const dotnet = (await bootsharp.dotnet.getMain(root)).dotnet;
                return await dotnet.withConfig(<never>customs.config).create();
            }),
            import: vi.fn(),
            run: vi.fn(),
            export: vi.fn()
        };
        await bootsharp.boot(customs);
        expect(customs.create).toHaveBeenCalledWith(customs.config);
        expect(customs.import).toHaveBeenCalledOnce();
        expect(customs.run).toHaveBeenCalledOnce();
        expect(customs.export).toHaveBeenCalledOnce();
    });
    it("can boot when program has no exports", async () => {
        const { side: { bootsharp }, root } = await setup();
        const options: BootOptions = {
            create: vi.fn(async () => {
                const cfg = await bootsharp.dotnet.buildConfig(bootsharp.resources, root);
                const dotnet = (await bootsharp.dotnet.getMain(root)).dotnet;
                const runtime = await dotnet.withConfig(cfg).create();
                runtime.getAssemblyExports = () => Promise.resolve({});
                return runtime;
            }),
            root
        };
        await bootsharp.boot(options);
    });
    it("resolves worker module in multithreading mode", async () => {
        const { side: { bootsharp }, root } = await setup();
        vi.doMock("../cs/Test/bin/sideload/dotnet.g", () => ({ mt: true }));
        const config = await bootsharp.dotnet.buildConfig(bootsharp.resources, root);
        expect(config.assets!.some(a => a.behavior === "js-module-threads")).toBeTruthy();
        vi.doUnmock("../cs/Test/bin/sideload/dotnet.g");
    });
});

describe("boot status", () => {
    it("is standby by default", async () => {
        const { side: { bootsharp } } = await setup();
        expect(bootsharp.getStatus()).toStrictEqual(bootsharp.BootStatus.Standby);
    });
    it("transitions to booting and then to booted", async () => {
        const { side: { bootsharp }, root } = await setup();
        const promise = bootsharp.boot({ root });
        expect(bootsharp.getStatus()).toStrictEqual(bootsharp.BootStatus.Booting);
        await promise;
        expect(bootsharp.getStatus()).toStrictEqual(bootsharp.BootStatus.Booted);
    });
    it("transitions to standby on exit", async () => {
        const { side: { bootsharp }, root } = await setup();
        await bootsharp.boot({ root });
        expect(bootsharp.getStatus()).toStrictEqual(bootsharp.BootStatus.Booted);
        await bootsharp.exit();
        expect(bootsharp.getStatus()).toStrictEqual(bootsharp.BootStatus.Standby);
    });
});
