import { describe, it, expect } from "vitest";
import { bootsharp, getDeclarations } from "../cs";

describe("export", () => {
    it("exports bootsharp api", () => {
        expect(bootsharp.boot).toBeTypeOf("function");
        expect(bootsharp.exit).toBeTypeOf("function");
        expect(bootsharp.resources).toBeTypeOf("object");
    });
    it("exports dotnet host builder", () => {
        expect(bootsharp.dotnet.withConfig).toBeTypeOf("function");
    });
    it("exports documentation declarations", () => {
        expect(getDeclarations()).toContain(`Sample class documentation.`);
    });
});
