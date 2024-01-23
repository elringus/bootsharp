import { describe, expect, it } from "vitest";
import { Event } from "../cs";

describe("event", () => {
    it("can broadcast without subscribers", () => {
        new Event().broadcast();
    });
    it("doesn't mind unsubscribing null handler", () => {
        new Event().unsubscribe(<never>null);
    });
    it("warns when unsubscribing handler which is not subscribed", () => {
        let warning;
        new Event({ warn: msg => warning = msg }).unsubscribe(<never>it);
        expect(warning).include("handler is not subscribed");
    });
    it("warns when subscribing handler which is already subscribed", () => {
        let warning;
        const event = new Event({ warn: msg => warning = msg });
        event.subscribe(<never>it);
        event.subscribe(<never>it);
        expect(warning).include("handler is already subscribed");
    });
    it("invokes subscribed handlers in order", () => {
        let result = "";
        const event = new Event();
        event.subscribe(() => result = "foo");
        event.subscribe(() => result = "bar");
        event.broadcast();
        expect(result).toStrictEqual("bar");
    });
    it("doesn't invoke un-subscribed handler", () => {
        let result = false;
        const event = new Event();
        const handler = (v: unknown) => result = <never>v;
        event.subscribe(handler);
        event.broadcast(true);
        event.unsubscribe(handler);
        event.broadcast(false);
        expect(result).toStrictEqual(true);
    });
    it("delivers broadcast argument to the handlers", () => {
        let result = "";
        const event = new Event();
        event.subscribe(v => result = <never>v);
        event.broadcast("foo");
        expect(result).toStrictEqual("foo");
    });
    it("can broadcast multiple arguments", () => {
        let resultA, resultB;
        const event = new Event();
        event.subscribe(function (a, b) {
            resultA = a;
            resultB = b;
        });
        event.broadcast(["foo", "bar", undefined, null], "nya");
        expect(resultA).toStrictEqual(["foo", "bar", undefined, null]);
        expect(resultB).toStrictEqual("nya");
    });
    it("doesnt add same handlers multiple times", () => {
        let result = 0;
        const event = new Event({ warn: () => {} });
        const incrementer = () => result++;
        for (let i = 0; i < 10; i++)
            event.subscribe(incrementer);
        event.broadcast();
        expect(result).toStrictEqual(1);
    });
    it("can un/subscribe by id", () => {
        let result = 0;
        const event = new Event();
        const incrementer = () => result++;
        for (let i = 0; i < 10; i++)
            event.subscribeById(i.toString(), incrementer);
        event.unsubscribeById("0");
        event.broadcast();
        expect(result).toStrictEqual(9);
    });
    it("returns undefined last args until no broadcasts performed", () => {
        expect(new Event().last).toBeUndefined();
    });
    it("returns args of the last broadcasts", () => {
        const event = new Event();
        event.broadcast("foo");
        event.broadcast("bar");
        expect(event.last).toStrictEqual(["bar"]);
    });
});
