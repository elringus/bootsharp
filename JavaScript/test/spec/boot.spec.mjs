import { expect, describe, it, vi } from "vitest";
import path from "node:path";

async function setup() {
    // dotnet merges with the host node process, so it's not possible
    // to exit w/o killing the test process (which is bound to test file);
    // this is a workaround to simulate clean environment in each test
    vi.resetModules();
    const cs = await import("../cs.mjs");
    cs.Test.onMainInvoked = vi.fn();
    return { bootsharp: cs.default, ...cs, Test: cs.Test, bins: cs.bins };
}

describe("boot", () => {
    it("can boot while streaming bins from root", async () => {
        const { bootsharp, Test } = await setup();
        bootsharp.resources.root = "./bin";
        await bootsharp.boot({});
        expect(Test.$onMainInvoked).toHaveBeenCalledOnce();
    });
    it("can boot with base64 content", async () => {
        const { bootsharp, Test, bins } = await setup();
        bootsharp.resources.wasm.content = bins.wasm.toString("base64");
        for (const asm of bootsharp.resources.assemblies)
            asm.content = bins.assemblies.find(a => a.name === asm.name).content.toString("base64");
        await bootsharp.boot({});
        expect(Test.$onMainInvoked).toHaveBeenCalledOnce();
    });
    it("can boot with base64 content w/o native encoder available", async () => {
        const { bootsharp, Test, bins } = await setup();
        global.Buffer = undefined;
        bootsharp.resources.wasm.content = bins.wasm.toString("base64");
        for (const asm of bootsharp.resources.assemblies)
            asm.content = bins.assemblies.find(a => a.name === asm.name).content.toString("base64");
        await bootsharp.boot({});
        expect(Test.$onMainInvoked).toHaveBeenCalledOnce();
    });
    it("attempts to use atob when window is defined in global", async () => {
        const { bootsharp, bins } = await setup();
        // noinspection JSValidateTypes
        global.window = { atob: vi.fn() };
        bootsharp.resources.wasm.content = bins.wasm.toString("base64");
        for (const asm of bootsharp.resources.assemblies)
            asm.content = bins.assemblies.find(a => a.name === asm.name).content.toString("base64");
        try { await bootsharp.boot({}); }
        catch {}
        expect(global.window.atob).toHaveBeenCalledOnce();
    });
    it("invokes dotnet.exit on exit", async () => {
        const exitMock = vi.fn();
        vi.doMock("../cs/Test/bin/bootsharp/dotnet", () => ({ exit: exitMock, dotnet: {} }));
        const { bootsharp } = await setup();
        bootsharp.exit();
        expect(exitMock).toHaveBeenCalledOnce();
        vi.doUnmock("../cs/Test/bin/bootsharp/dotnet");
    });
    it("respects boot customs", async () => {
        const { bootsharp, bins } = await setup();
        const customs = {
            config: {
                mainAssemblyName: bins.entryAssemblyName,
                assets: [
                    {
                        name: path.resolve("test/cs/Test/bin/bootsharp/dotnet.runtime.g.mjs"),
                        behavior: "js-module-runtime"
                    },
                    {
                        name: path.resolve("test/cs/Test/bin/bootsharp/dotnet.native.g.mjs"),
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
            create: vi.fn(() => bootsharp.dotnet.builder.withConfig(customs.config).create()),
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
        const { bootsharp } = await setup();
        bootsharp.resources.root = "./bin";
        const promise = bootsharp.boot({});
        expect(bootsharp.getStatus()).toStrictEqual(bootsharp.BootStatus.Booting);
        await promise;
        expect(bootsharp.getStatus()).toStrictEqual(bootsharp.BootStatus.Booted);
    });
    it("transitions to standby on exit", async () => {
        const { bootsharp } = await setup();
        bootsharp.resources.root = "./bin";
        await bootsharp.boot({});
        expect(bootsharp.getStatus()).toStrictEqual(bootsharp.BootStatus.Booted);
        bootsharp.exit();
        expect(bootsharp.getStatus()).toStrictEqual(bootsharp.BootStatus.Standby);
    });
});
