import assert from "node:assert";
import { describe, it } from "node:test";
import bootsharp, { getBinaries, Test } from "../cs.mjs";

describe("boot", () => {
    it("throws when missing WASM boot resource", async () => {
        await assert.rejects(bootsharp.boot, /Missing WASM boot resource/);
    });
    it("throws when missing assembly boot resource", async () => {
        bootsharp.resources.wasm.content = getBinaries().wasm;
        await assert.rejects(bootsharp.boot, /Missing '.+\.wasm' assembly boot resource/);
    });
    it("can boot with byte content", async () => {
        const bins = getBinaries();
        bootsharp.resources.wasm.content = bins.wasm;
        for (const asm of bootsharp.resources.assemblies)
            asm.content = bins.assemblies.find(a => a.name === asm.name).content;
        await assert.doesNotReject(bootsharp.boot);
    });
    it("can boot with base64 content", async () => {
        const bins = getBinaries();
        bootsharp.resources.wasm.content = bins.wasm.toString("base64");
        for (const asm of bootsharp.resources.assemblies)
            asm.content = bins.assemblies.find(a => a.name === asm.name).content.toString("base64");
        await assert.doesNotReject(bootsharp.boot);
    });
    it("invokes entry point on boot", async () => {
        assert(Test.isMainInvoked());
    });
});
