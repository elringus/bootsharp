import assert from "node:assert";
import { describe, it, before, after } from "node:test";
import { boot, exit, Test } from "../cs.mjs";
import ws, { WebSocketServer } from "ws";

// .NET requires ws package when running on node:
// https://github.com/dotnet/runtime/blob/main/src/mono/wasm/features.md#websocket
global.WebSocket = ws;

describe("platform", () => {
    before(boot);
    after(exit);
    it("can provide unique guid", () => {
        const guid1 = Test.getGuid();
        const guid2 = Test.getGuid();
        assert.strictEqual(guid1.length, 36);
        assert.strictEqual(guid2.length, 36);
        assert.notStrictEqual(guid1, guid2);
    });
    it("can connect via websocket", async () => {
        const wss = new WebSocketServer({ port: 8080 });
        wss.on("connection", socket => socket.on("message", data => socket.send(data)));
        const echo = await Test.echoViaWebSocket("ws://localhost:8080", "foo", 1);
        assert.deepStrictEqual(echo, "foo");
        wss.close();
    });
});
