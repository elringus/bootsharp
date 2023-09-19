const assert = require("node:assert");
const { describe, it, before, after } = require("node:test");
const dotnet = require("../dist/dotnet");
const { bootTest } = require("./cs");

const invoke = (name, ...args) => dotnet.invoke("Test.Main", name, ...args);
const invokeAsync = (name, ...args) => dotnet.invokeAsync("Test.Main", name, ...args);

describe("platform", () => {
    before(bootTest);
    after(dotnet.terminate);
    it("can provide guid", () => {
        const guid1 = invoke("GetGuid");
        const guid2 = invoke("GetGuid");
        assert.deepStrictEqual(guid1.length, 36);
        assert.deepStrictEqual(guid2.length, 36);
        assert.notDeepEqual(guid1, guid2);
    });
    it("can connect via websocket", async () => {
        global.WebSocket = require("ws");
        const wss = new WebSocket.Server({ port: 8080 });
        wss.on("connection", socket => socket.on("message", data => socket.send(data)));
        const echo = await invokeAsync("EchoViaWebSocket", "ws://localhost:8080", "foo", 1);
        assert.deepStrictEqual(echo, "foo");
        wss.close();
    });
});
