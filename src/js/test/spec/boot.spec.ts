import { describe, expect, it, vi } from "vitest";
import { resolve } from "node:path";
import type { BootOptions } from "../cs/Test/bin/sideload";

async function setup() {
    // dotnet merges with the host node process, so it's not possible
    // to exit w/o killing the test process (which is bound to test file);
    // this is a workaround to simulate clean environment in each test
    vi.resetModules();
    const cs = await import("../cs");
    cs.SideloadTest.Program.onMainInvoked = vi.fn();
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
        expect((await bootsharp.dotnet.getMain(root)).embedded).toBeUndefined();
        expect((await bootsharp.dotnet.getNative(root)).embedded).toBeUndefined();
        expect((await bootsharp.dotnet.getRuntime(root)).embedded).toBeUndefined();
    });
    it("defines module exports when root is not specified", async () => {
        const { bootsharp } = await setup();
        const module = await import("../cs/Test/bin/sideload");
        const config = await module.default.dotnet.buildConfig(bootsharp.resources);
        expect(config.assets![0].moduleExports).toBeDefined();
        expect(config.assets![1].moduleExports).toBeDefined();
        expect(config.assets![2].moduleExports).toBeDefined();
    });
    it("overrides name to url in multithreading mode", async () => {
        const { bootsharp, root } = await setup();
        vi.doMock("../cs/Test/bin/sideload/dotnet.g", () => ({ mt: true }));
        const module = await import("../cs/Test/bin/sideload");
        const config = await module.default.dotnet.buildConfig(bootsharp.resources, root);
        expect(config.assets![0].name.endsWith("/bin/dotnet.js")).toBeTruthy();
        expect(config.assets![1].name.endsWith("/bin/dotnet.native.js")).toBeTruthy();
        expect(config.assets![2].name.endsWith("/bin/dotnet.runtime.js")).toBeTruthy();
        vi.doUnmock("../cs/Test/bin/sideload/dotnet.g");
    });
    it("can boot in embedded mode", async () => {
        vi.resetModules();
        const cs = await import("../cs");
        cs.EmbeddedTest.Program.onMainInvoked = vi.fn();
        await cs.embedded.boot({});
        expect(cs.EmbeddedTest.Program.onMainInvoked).toHaveBeenCalledOnce();
    });
    it("can boot while streaming bins from root", async () => {
        const { bootsharp, root, Test } = await setup();
        await bootsharp.boot({ root });
        expect(Test.Program.onMainInvoked).toHaveBeenCalledOnce();
    });
    it("can boot with bins content pre-assigned", async () => {
        const { bootsharp, Test, root, bins, any } = await setup();
        const resources = { ...bootsharp.resources };
        any(resources.wasm).content = bins.wasm;
        for (const asm of resources.assemblies)
            any(asm).content = bins.assemblies.find(a => a.name === asm.name)!.content;
        await bootsharp.boot({ resources, root });
        expect(Test.Program.onMainInvoked).toHaveBeenCalledOnce();
    });
    it("can boot with base64 content", async () => {
        const { bootsharp, Test, root, bins, any } = await setup();
        const resources = { ...bootsharp.resources };
        any(resources.wasm).content = bins.wasm.toString("base64");
        for (const asm of resources.assemblies)
            any(asm).content = bins.assemblies.find(a => a.name === asm.name)!.content.toString("base64");
        await bootsharp.boot({ resources, root });
        expect(Test.Program.onMainInvoked).toHaveBeenCalledOnce();
    });
    it("can boot with base64 content w/o native encoder available", async () => {
        const { bootsharp, Test, root, bins, any } = await setup();
        any(global).Buffer = undefined;
        const resources = { ...bootsharp.resources };
        any(resources.wasm).content = bins.wasm.toString("base64");
        for (const asm of resources.assemblies)
            any(asm).content = bins.assemblies.find(a => a.name === asm.name)!.content.toString("base64");
        await bootsharp.boot({ resources, root });
        expect(Test.Program.onMainInvoked).toHaveBeenCalledOnce();
    });
    it("attempts to use atob when window is defined in global", async () => {
        const { bootsharp, root, bins, any } = await setup();
        any(global).window = { atob: vi.fn() };
        const resources = { ...bootsharp.resources };
        any(resources.wasm).content = bins.wasm.toString("base64");
        for (const asm of resources.assemblies)
            any(asm).content = bins.assemblies.find(a => a.name === asm.name)!.content.toString("base64");
        try { await bootsharp.boot({ resources, root }); }
        catch {}
        expect(global.window.atob).toHaveBeenCalledOnce();
    });
    it("throws when boot invoked while booted", async () => {
        const { bootsharp, root } = await setup();
        await bootsharp.boot({ root });
        await expect(bootsharp.boot).rejects.toThrow(/already booted/);
    });
    it("throws when boot invoked while booting", async () => {
        const { bootsharp, root } = await setup();
        const boot = bootsharp.boot({ root });
        await expect(bootsharp.boot).rejects.toThrow(/already booting/);
        await boot;
    });
    it("throws when exit invoked while not booted", async () => {
        const { bootsharp } = await setup();
        await expect(bootsharp.exit).rejects.toThrow(/not booted/);
    });
    it("can exit when booted", async () => {
        const { bootsharp, root } = await setup();
        await bootsharp.boot({ root });
        await bootsharp.exit();
        expect(bootsharp.getStatus()).toStrictEqual(0);
    });
    it("respects boot customs", async () => {
        const { bootsharp, bins, root } = await setup();
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
                        buffer: bins.wasm,
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
        const { bootsharp, root } = await setup();
        const options: BootOptions = {
            create: vi.fn(async () => {
                const cfg = await bootsharp.dotnet.buildConfig(bootsharp.resources, root);
                const dotnet = (await bootsharp.dotnet.getMain(root)).dotnet;
                const runtime = await dotnet.withConfig(cfg).create();
                runtime.getAssemblyExports = () => Promise.resolve({});
                return runtime;
            })
        };
        await bootsharp.boot(options);
    });
});

describe("boot status", () => {
    it("is standby by default", async () => {
        const { bootsharp } = await setup();
        expect(bootsharp.getStatus()).toStrictEqual(bootsharp.BootStatus.Standby);
    });
    it("transitions to booting and then to booted", async () => {
        const { bootsharp, root } = await setup();
        const promise = bootsharp.boot({ root });
        expect(bootsharp.getStatus()).toStrictEqual(bootsharp.BootStatus.Booting);
        await promise;
        expect(bootsharp.getStatus()).toStrictEqual(bootsharp.BootStatus.Booted);
    });
    it("transitions to standby on exit", async () => {
        const { bootsharp, root } = await setup();
        await bootsharp.boot({ root });
        expect(bootsharp.getStatus()).toStrictEqual(bootsharp.BootStatus.Booted);
        await bootsharp.exit();
        expect(bootsharp.getStatus()).toStrictEqual(bootsharp.BootStatus.Standby);
    });
});
