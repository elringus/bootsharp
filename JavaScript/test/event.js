const assert = require("assert");
const { Event } = require("../dist/dotnet");

describe("event", () => {
    it("can broadcast without subscribers", () => {
        assert.doesNotThrow(() => new Event().broadcast());
    });
    it("doesn't mind unsubscribing null handler", () => {
        assert.doesNotThrow(() => new Event().unsubscribe(null));
    });
    it("warns when unsubscribing handler which is not subscribed", () => {
        let warning;
        new Event(msg => warning = msg).unsubscribe(it);
        assert(warning.includes("handler is not subscribed"));
    });
    it("warns when subscribing handler which is already subscribed", () => {
        let warning;
        const event = new Event(msg => warning = msg);
        event.subscribe(it);
        event.subscribe(it);
        assert(warning.includes("handler is already subscribed"));
    });
    it("invokes subscribed handlers in order", () => {
        let result = "";
        const event = new Event();
        event.subscribe(() => result = "foo");
        event.subscribe(() => result = "bar");
        event.broadcast();
        assert.deepStrictEqual(result, "bar");
    });
    it("doesn't invoke un-subscribed handler", () => {
        let result = false;
        const event = new Event();
        const handler = v => result = v;
        event.subscribe(handler);
        event.broadcast(true);
        event.unsubscribe(handler);
        event.broadcast(false);
        assert.deepStrictEqual(result, true);
    });
    it("delivers broadcast argument to the handlers", () => {
        let result = "";
        const event = new Event();
        event.subscribe(v => result = v);
        event.broadcast("foo");
        assert.deepStrictEqual(result, "foo");
    });
    it("doesnt add same handlers multiple times", () => {
        let result = 0;
        const event = new Event(null);
        const incrementer = () => result++;
        for (let i = 0; i < 10; i++)
            event.subscribe(incrementer);
        event.broadcast();
        assert.deepStrictEqual(result, 1);
    });
    it("can broadcast multiple args", () => {
        let resultA, resultB;
        const event = new Event();
        event.subscribe(function (a, b) {
            resultA = a;
            resultB = b;
        });
        event.broadcast(["foo", "bar"], "nya");
        assert.deepStrictEqual(resultA, ["foo", "bar"]);
        assert.deepStrictEqual(resultB, "nya");
    });
    it("can un/subscribe by id", () => {
        let result = 0;
        const event = new Event(null);
        const incrementer = () => result++;
        for (let i = 0; i < 10; i++)
            event.subscribeById(i.toString(), incrementer);
        event.unsubscribeById("0");
        event.broadcast();
        assert.deepStrictEqual(result, 9);
    });
    it("returns undefined last args until no broadcasts performed", () => {
        assert.deepStrictEqual(new Event().getLast(), undefined);
    });
    it("returns args of the last broadcasts", () => {
        const event = new Event();
        event.broadcast("foo");
        event.broadcast("bar", "nya");
        assert.deepStrictEqual(event.getLast(), ["bar", "nya"]);
    });
});
