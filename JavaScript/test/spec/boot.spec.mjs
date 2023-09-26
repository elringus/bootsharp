import { expect, beforeEach, describe, it, vi } from "vitest";
import bootsharp, { bins, Test } from "../cs.mjs";

beforeEach(() => Test.onMainInvoked = vi.fn());

describe("boot", () => {
    it("throws when missing WASM boot resource", async () => {
        bootsharp.resources.wasm.content = [];
        await expect(bootsharp.boot).rejects.toContain(/Missing WASM boot resource/);
        expect(Test.$onMainInvoked).not.toHaveBeenCalled();
    });
    it("throws when missing assembly boot resource", async () => {
        bootsharp.resources.wasm.content = bins.wasm;
        bootsharp.resources.assemblies[0].content = [];
        await expect(bootsharp.boot).rejects.toContain(/Missing '.+\.wasm' assembly boot resource/);
        expect(Test.$onMainInvoked).not.toHaveBeenCalled();
    });
    it("can boot with byte content", async () => {
        bootsharp.resources.wasm.content = bins.wasm;
        for (const asm of bootsharp.resources.assemblies)
            asm.content = bins.assemblies.find(a => a.name === asm.name).content;
        await bootsharp.boot({});
        expect(Test.$onMainInvoked).toHaveBeenCalledOnce();
    });
    it("can boot with base64 content", async () => {
        bootsharp.resources.wasm.content = bins.wasm.toString("base64");
        for (const asm of bootsharp.resources.assemblies)
            asm.content = bins.assemblies.find(a => a.name === asm.name).content.toString("base64");
        await bootsharp.boot({});
        expect(Test.$onMainInvoked).toHaveBeenCalledOnce();
    });
    // it("can boot with base64 content w/o native encoder available", async () => {
    //     global.window = undefined;
    //     global.Buffer = undefined;
    //     bootsharp.resources.wasm.content = bins.wasm.toString("base64");
    //     for (const asm of bootsharp.resources.assemblies)
    //         asm.content = bins.assemblies.find(a => a.name === asm.name).content.toString("base64");
    //     bootsharp.exit();
    //     await bootsharp.boot({});
    //     expect(Test.$onMainInvoked).toHaveBeenCalledOnce();
    // });
});
