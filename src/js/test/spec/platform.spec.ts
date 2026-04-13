import { describe, it, beforeAll, expect } from "vitest";
import { WebSocket, WebSocketServer } from "ws";
import { Test, bootSideload, any } from "../cs";

describe("platform", () => {
    beforeAll(bootSideload);
    it("can provide unique guid", () => {
        const guid1 = Test.Platform.getGuid();
        const guid2 = Test.Platform.getGuid();
        expect(guid1.length).toStrictEqual(36);
        expect(guid2.length).toStrictEqual(36);
        expect(guid1).not.toStrictEqual(guid2);
    });
    it("can communicate via websocket", async () => {
        // .NET requires ws package when running on node:
        // https://github.com/dotnet/runtime/blob/main/src/mono/wasm/features.md#websocket
        any(global).WebSocket = WebSocket;
        const wss = new WebSocketServer({ port: 0 });
        wss.on("connection", socket => socket.on("message", socket.send));
        await new Promise<void>(resolve => wss.once("listening", resolve));
        const port = (wss.address() as { port: number }).port;
        expect(await Test.Platform.echoWebSocket(`ws://localhost:${port}`, "foo", 3000)).toStrictEqual("foo");
        wss.close();
    });
});
