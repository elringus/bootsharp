import { strictEqual, notStrictEqual } from "node:assert";
import { describe, it, before, after } from "node:test";
import { boot, exit, Test } from "../cs.mjs";
import { Worker } from "node:worker_threads";
import ws from "ws";

describe("platform", () => {
    before(boot);
    after(exit);
    it("can provide unique guid", () => {
        const guid1 = Test.getGuid();
        const guid2 = Test.getGuid();
        strictEqual(guid1.length, 36);
        strictEqual(guid2.length, 36);
        notStrictEqual(guid1, guid2);
    });
    it("can communicate via websocket", async () => {
        // .NET requires ws package when running on node:
        // https://github.com/dotnet/runtime/blob/main/src/mono/wasm/features.md#websocket
        global.WebSocket = ws;
        let ready, preparing = new Promise(r => ready = r);
        let echo, echoing = new Promise(r => echo = r);
        Test.onMessage.subscribe(echo);
        const worker = new Worker("./tests/wss.mjs");
        worker.on("message", msg => msg === "ready" && ready());
        await preparing;
        Test.echoWebSocket("ws://localhost:8080", "foo", 3000);
        strictEqual(await echoing, "foo");
        await worker.terminate();
    });
});
