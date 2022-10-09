const assert = require("assert");
const { Event } = require("../dist/dotnet");

describe("event", () => {
    it("can broadcast without subscribers", () => {
        assert.doesNotThrow(() => new Event().broadcast());
    });
    it("doesn't mind unsubscribing non-existent handler", () => {
        assert.doesNotThrow(() => new Event().unsubscribe(null));
        assert.doesNotThrow(() => new Event().unsubscribeById(""));
    });
    it("invokes subscribed handlers in order", () => {
        let result = "";
        const evt = new Event();
        evt.subscribe(() => result = "foo");
        evt.subscribe(() => result = "bar");
        evt.broadcast();
        assert.deepStrictEqual(result, "bar");
    });
    it("doesn't invoke un-subscribed handler", () => {
        let result = false;
        const evt = new Event();
        const handler = v => result = v;
        evt.subscribe(handler);
        evt.broadcast(true);
        evt.unsubscribe(handler);
        evt.broadcast(false);
        assert.deepStrictEqual(result, true);
    });
    it("delivers broadcast argument to the handlers", () => {
        let result = "";
        const evt = new Event();
        evt.subscribe(v => result = v);
        evt.broadcast("foo");
        assert.deepStrictEqual(result, "foo");
    });
    it("doesnt add same handlers multiple times", () => {
        let result = 0;
        const evt = new Event();
        const incrementer = () => result++;
        for (let i = 0; i < 10; i++)
            evt.subscribe(incrementer);
        evt.broadcast();
        assert.deepStrictEqual(result, 1);
    });
    it("can broadcast multiple args", () => {
        let resultA, resultB;
        const evt = new Event();
        evt.subscribe(function (a, b) {
            resultA = a;
            resultB = b;
        });
        evt.broadcast(["foo", "bar"], "nya");
        assert.deepStrictEqual(resultA, ["foo", "bar"]);
        assert.deepStrictEqual(resultB, "nya");
    });
    it("can un/subscribe by id", () => {
        let result = 0;
        const evt = new Event();
        const incrementer = () => result++;
        for (let i = 0; i < 10; i++)
            evt.subscribeById(i.toString(), incrementer);
        evt.unsubscribeById("0");
        evt.broadcast();
        assert.deepStrictEqual(result, 9);
    });
});
