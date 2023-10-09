import { describe, expect, it, vi } from "vitest";
import { resolve } from "node:path";

async function setup() {
    // dotnet merges with the host node process, so it's not possible
    // to exit w/o killing the test process (which is bound to test file);
    // this is a workaround to simulate clean environment in each test
    vi.resetModules();
    const cs = await import("../cs.mjs");
    cs.SideloadTest.onMainInvoked = vi.fn();
    return { ...cs, bootsharp: cs.sideload, Test: cs.SideloadTest };
}

describe("boot", () => {
    it("uses embedded modules when root is not specified", async () => {
        const { bootsharp } = await setup();
        expect((await bootsharp.dotnet.getMain()).embedded).toStrictEqual(false);
        expect((await bootsharp.dotnet.getNative()).embedded).toStrictEqual(false);
        expect((await bootsharp.dotnet.getRuntime()).embedded).toStrictEqual(false);
    });
    it("uses sideload modules when root is specified", async () => {
        const { bootsharp, root } = await setup();
        bootsharp.resources.root = root;
        expect((await bootsharp.dotnet.getMain()).embedded).toBeUndefined();
        expect((await bootsharp.dotnet.getNative()).embedded).toBeUndefined();
        expect((await bootsharp.dotnet.getRuntime()).embedded).toBeUndefined();
    });
    it("defines module exports when root is not specified", async () => {
        await setup();
        const module = await import("../cs/Test/bin/sideload/config");
        const config = await module.buildConfig();
        expect(config.assets[0].moduleExports).toBeDefined();
        expect(config.assets[1].moduleExports).toBeDefined();
        expect(config.assets[2].moduleExports).toBeDefined();
    });
    it("can boot in embedded mode", async () => {
        vi.resetModules();
        const cs = await import("../cs.mjs");
        cs.EmbeddedTest.onMainInvoked = vi.fn();
        await cs.embedded.boot({});
        expect(cs.EmbeddedTest.$onMainInvoked).toHaveBeenCalledOnce();
    });
    it("can boot while streaming bins from root", async () => {
        const { bootsharp, root, Test } = await setup();
        bootsharp.resources.root = root;
        await bootsharp.boot({});
        expect(Test.$onMainInvoked).toHaveBeenCalledOnce();
    });
    it("can boot with bins content pre-assigned", async () => {
        const { bootsharp, Test, root, bins } = await setup();
        bootsharp.resources.root = root;
        bootsharp.resources.wasm.content = bins.wasm;
        for (const asm of bootsharp.resources.assemblies)
            asm.content = bins.assemblies.find(a => a.name === asm.name).content;
        await bootsharp.boot({});
        expect(Test.$onMainInvoked).toHaveBeenCalledOnce();
    });
    it("can boot with base64 content", async () => {
        const { bootsharp, Test, root, bins } = await setup();
        bootsharp.resources.root = root;
        bootsharp.resources.wasm.content = bins.wasm.toString("base64");
        for (const asm of bootsharp.resources.assemblies)
            asm.content = bins.assemblies.find(a => a.name === asm.name).content.toString("base64");
        await bootsharp.boot({});
        expect(Test.$onMainInvoked).toHaveBeenCalledOnce();
    });
    it("can boot with base64 content w/o native encoder available", async () => {
        const { bootsharp, Test, root, bins } = await setup();
        global.Buffer = undefined;
        bootsharp.resources.root = root;
        bootsharp.resources.wasm.content = bins.wasm.toString("base64");
        for (const asm of bootsharp.resources.assemblies)
            asm.content = bins.assemblies.find(a => a.name === asm.name).content.toString("base64");
        await bootsharp.boot({});
        expect(Test.$onMainInvoked).toHaveBeenCalledOnce();
    });
    it("attempts to use atob when window is defined in global", async () => {
        const { bootsharp, root, bins } = await setup();
        // noinspection JSValidateTypes
        global.window = { atob: vi.fn() };
        bootsharp.resources.root = root;
        bootsharp.resources.wasm.content = bins.wasm.toString("base64");
        for (const asm of bootsharp.resources.assemblies)
            asm.content = bins.assemblies.find(a => a.name === asm.name).content.toString("base64");
        try { await bootsharp.boot({}); }
        catch {}
        expect(global.window.atob).toHaveBeenCalledOnce();
    });
    it("invokes dotnet.exit on exit", async () => {
        const exitMock = vi.fn();
        vi.doMock("../cs/Test/bin/sideload/dotnet.g", () => ({ exit: exitMock, dotnet: {} }));
        const { bootsharp } = await setup();
        await bootsharp.exit();
        expect(exitMock).toHaveBeenCalledOnce();
        vi.doUnmock("../cs/Test/bin/sideload/dotnet.g");
    });
    it("respects boot customs", async () => {
        const { bootsharp, bins } = await setup();
        const customs = {
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
                        buffer: bins.wasm,
                        behavior: "dotnetwasm"
                    },
                    ...bins.assemblies.map(a => ({ name: a.name, buffer: a.content, behavior: "assembly" }))
                ]
            },
            create: vi.fn(async () => (await import("../cs/Test/bin/sideload/bin/dotnet.js")).dotnet.withConfig(customs.config).create()),
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
});

describe("boot status", () => {
    it("is standby by default", async () => {
        const { bootsharp } = await setup();
        expect(bootsharp.getStatus()).toStrictEqual(bootsharp.BootStatus.Standby);
    });
    it("transitions to booting and then to booted", async () => {
        const { bootsharp, root } = await setup();
        bootsharp.resources.root = root;
        const promise = bootsharp.boot({});
        expect(bootsharp.getStatus()).toStrictEqual(bootsharp.BootStatus.Booting);
        await promise;
        expect(bootsharp.getStatus()).toStrictEqual(bootsharp.BootStatus.Booted);
    });
    it("transitions to standby on exit", async () => {
        const { bootsharp, root } = await setup();
        bootsharp.resources.root = root;
        await bootsharp.boot({});
        expect(bootsharp.getStatus()).toStrictEqual(bootsharp.BootStatus.Booted);
        await bootsharp.exit();
        expect(bootsharp.getStatus()).toStrictEqual(bootsharp.BootStatus.Standby);
    });
});
