import { describe, it, expect } from "vitest";
import { embedded, getDeclarations } from "../cs";

describe("export", () => {
    it("exports bootsharp api", () => {
        expect(embedded.boot).toBeTypeOf("function");
        expect(embedded.exit).toBeTypeOf("function");
        expect(embedded.resources).toBeTypeOf("object");
    });
    it("exports dotnet modules", () => {
        expect(embedded.dotnet.getMain).toBeTypeOf("function");
        expect(embedded.dotnet.getNative).toBeTypeOf("function");
        expect(embedded.dotnet.getRuntime).toBeTypeOf("function");
    });
    it("exports type declarations", () => {
        expect(getDeclarations()).toBeTruthy();
    });
    it("exports documentation declarations", () => {
        expect(getDeclarations()).toContain(`
/**
 * Invokable test API.
 */
export namespace Test.Invokable {
    `);
        expect(getDeclarations()).toContain(`
    /**
     * Joins two strings.
     * @param a First string.
     * @param b Second string.
     * @returns Joined string.
     */
    export function joinStrings(a: string, b: string): string;
    `);
    });
});
