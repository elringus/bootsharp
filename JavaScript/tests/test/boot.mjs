import { rejects, doesNotReject, strictEqual } from "node:assert";
import { beforeEach, describe, it, mock } from "node:test";
import bootsharp, { bins, exit, Test } from "../cs.mjs";

beforeEach(() => Test.onMainInvoked = mock.fn());

describe("boot", () => {
    it("throws when missing WASM boot resource", async () => {
        bootsharp.resources.wasm.content = [];
        await rejects(bootsharp.boot, /Missing WASM boot resource/);
        strictEqual(Test.$onMainInvoked.mock.calls.length, 0);
    });
    it("throws when missing assembly boot resource", async () => {
        bootsharp.resources.wasm.content = bins.wasm;
        bootsharp.resources.assemblies[0].content = [];
        await rejects(bootsharp.boot, /Missing '.+\.wasm' assembly boot resource/);
        strictEqual(Test.$onMainInvoked.mock.calls.length, 0);
    });
    it("can boot with byte content", async () => {
        bootsharp.resources.wasm.content = bins.wasm;
        for (const asm of bootsharp.resources.assemblies)
            asm.content = bins.assemblies.find(a => a.name === asm.name).content;
        await doesNotReject(bootsharp.boot);
        strictEqual(Test.$onMainInvoked.mock.calls.length, 1);
        exit();
    });
    it("can boot with base64 content", async () => {
        bootsharp.resources.wasm.content = bins.wasm.toString("base64");
        for (const asm of bootsharp.resources.assemblies)
            asm.content = bins.assemblies.find(a => a.name === asm.name).content.toString("base64");
        await doesNotReject(bootsharp.boot);
        strictEqual(Test.$onMainInvoked.mock.calls.length, 1);
        exit();
    });
    it("can boot with base64 content w/o native encoder available", async () => {
        global.window = undefined;
        global.Buffer = undefined;
        bootsharp.resources.wasm.content = bins.wasm.toString("base64");
        for (const asm of bootsharp.resources.assemblies)
            asm.content = bins.assemblies.find(a => a.name === asm.name).content.toString("base64");
        await doesNotReject(() => bootsharp.boot({}));
        strictEqual(Test.$onMainInvoked.mock.calls.length, 1);
        exit();
    });
});
