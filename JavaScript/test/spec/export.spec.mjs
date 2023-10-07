import { describe, it, expect } from "vitest";
import { embedded, getDeclarations } from "../cs.mjs";

describe("export", () => {
    it("exports bootsharp api", () => {
        expect(embedded.boot).toBeTypeOf("function");
        expect(embedded.exit).toBeTypeOf("function");
        expect(embedded.resources).toBeTypeOf("object");
    });
    it("exports dotnet api", () => {
        expect(embedded.dotnet.builder).toBeTypeOf("object");
        expect(embedded.dotnet.module).toBeTypeOf("object");
    });
    it("exports type declarations", () => {
        expect(getDeclarations()).toBeTruthy();
    });
});
