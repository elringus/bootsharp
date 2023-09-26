import { describe, it, expect } from "vitest";
import bootsharp, { getDeclarations } from "../cs.mjs";

describe("export", () => {
    it("exports bootsharp api", () => {
        expect(bootsharp.boot).toBeTypeOf("function");
        expect(bootsharp.exit).toBeTypeOf("function");
        expect(bootsharp.resources).toBeTypeOf("object");
    });
    it("exports dotnet api", () => {
        expect(bootsharp.dotnet.builder).toBeTypeOf("object");
        expect(bootsharp.dotnet.native).toBeTypeOf("object");
        expect(bootsharp.dotnet.runtime).toBeTypeOf("object");
    });
    it("exports type declarations", () => {
        expect(getDeclarations()).toBeTruthy();
    });
});
