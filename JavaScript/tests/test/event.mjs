import assert, { doesNotThrow, deepStrictEqual } from "node:assert";
import { describe, it } from "node:test";
import { Event } from "../cs.mjs";

describe("event", () => {
    it("can broadcast without subscribers", () => {
        doesNotThrow(() => new Event().broadcast());
    });
    it("doesn't mind unsubscribing null handler", () => {
        doesNotThrow(() => new Event().unsubscribe(null));
    });
    it("warns when unsubscribing handler which is not subscribed", () => {
        let warning;
        new Event({ warn: msg => warning = msg }).unsubscribe(it);
        assert(warning.includes("handler is not subscribed"));
    });
    it("warns when subscribing handler which is already subscribed", () => {
        let warning;
        const event = new Event({ warn: msg => warning = msg });
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
        deepStrictEqual(result, "bar");
    });
    it("doesn't invoke un-subscribed handler", () => {
        let result = false;
        const event = new Event();
        const handler = v => result = v;
        event.subscribe(handler);
        event.broadcast(true);
        event.unsubscribe(handler);
        event.broadcast(false);
        deepStrictEqual(result, true);
    });
    it("delivers broadcast argument to the handlers", () => {
        let result = "";
        const event = new Event();
        event.subscribe(v => result = v);
        event.broadcast("foo");
        deepStrictEqual(result, "foo");
    });
    it("doesnt add same handlers multiple times", () => {
        let result = 0;
        const event = new Event({ warn: () => {} });
        const incrementer = () => result++;
        for (let i = 0; i < 10; i++)
            event.subscribe(incrementer);
        event.broadcast();
        deepStrictEqual(result, 1);
    });
    it("can un/subscribe by id", () => {
        let result = 0;
        const event = new Event({ warn: () => {} });
        const incrementer = () => result++;
        for (let i = 0; i < 10; i++)
            event.subscribeById(i.toString(), incrementer);
        event.unsubscribeById("0");
        event.broadcast();
        deepStrictEqual(result, 9);
    });
    it("returns undefined last args until no broadcasts performed", () => {
        deepStrictEqual(new Event().last, undefined);
    });
    it("returns args of the last broadcasts", () => {
        const event = new Event();
        event.broadcast("foo");
        event.broadcast("bar");
        deepStrictEqual(event.last, "bar");
    });
    it("can transform payload", () => {
        const event = new Event({ convert: _ => "bar" });
        event.broadcast("foo");
        deepStrictEqual(event.last, "bar");
    });
});
