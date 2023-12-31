import { describe, it, expect } from "vitest";
import { embedded, getDeclarations } from "../cs.mjs";

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
});
