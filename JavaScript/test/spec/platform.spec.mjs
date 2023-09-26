import { describe, it, beforeAll, expect } from "vitest";
import { boot, Test } from "../cs.mjs";
import { Worker } from "node:worker_threads";
import ws from "ws";

describe("platform", () => {
    beforeAll(boot);
    it("can provide unique guid", () => {
        const guid1 = Test.getGuid();
        const guid2 = Test.getGuid();
        expect(guid1.length).toStrictEqual(36);
        expect(guid2.length).toStrictEqual(36);
        expect(guid1).not.toStrictEqual(guid2);
    });
    it("can communicate via websocket", async () => {
        // .NET requires ws package when running on node:
        // https://github.com/dotnet/runtime/blob/main/src/mono/wasm/features.md#websocket
        global.WebSocket = ws;
        let ready, preparing = new Promise(r => ready = r);
        let echo, echoing = new Promise(r => echo = r);
        Test.onMessage.subscribe(echo);
        const worker = new Worker("./test/wss.mjs");
        worker.on("message", msg => msg === "ready" && ready());
        await preparing;
        Test.echoWebSocket("ws://localhost:8080", "foo", 3000);
        expect(await echoing).toStrictEqual("foo");
        await worker.terminate();
    });
});
