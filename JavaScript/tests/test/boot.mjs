import assert from "node:assert";
import { beforeEach, describe, it, mock } from "node:test";
import { setTimeout } from "node:timers";
import bootsharp, { bins, Test } from "../cs.mjs";

beforeEach(() => Test.onMainInvoked = mock.fn());

describe("boot", () => {
    it("throws when missing WASM boot resource", async () => {
        bootsharp.resources.wasm.content = [];
        await assert.rejects(bootsharp.boot, /Missing WASM boot resource/);
        assert.strictEqual(Test.$onMainInvoked.mock.calls.length, 0);
    });
    it("throws when missing assembly boot resource", async () => {
        bootsharp.resources.wasm.content = bins.wasm;
        bootsharp.resources.assemblies[0].content = [];
        await assert.rejects(bootsharp.boot, /Missing '.+\.wasm' assembly boot resource/);
        assert.strictEqual(Test.$onMainInvoked.mock.calls.length, 0);
    });
    it("can boot with byte content", async () => {
        bootsharp.resources.wasm.content = bins.wasm;
        for (const asm of bootsharp.resources.assemblies)
            asm.content = bins.assemblies.find(a => a.name === asm.name).content;
        await assert.doesNotReject(bootsharp.boot);
        assert.strictEqual(Test.$onMainInvoked.mock.calls.length, 1);
        setTimeout(bootsharp.exit, 0);
    });
    it("can boot with base64 content", async () => {
        bootsharp.resources.wasm.content = bins.wasm.toString("base64");
        for (const asm of bootsharp.resources.assemblies)
            asm.content = bins.assemblies.find(a => a.name === asm.name).content.toString("base64");
        await assert.doesNotReject(bootsharp.boot);
        assert.strictEqual(Test.$onMainInvoked.mock.calls.length, 1);
        setTimeout(bootsharp.exit, 0);
    });
});
