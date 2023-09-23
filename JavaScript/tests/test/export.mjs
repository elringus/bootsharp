import assert, { strictEqual } from "node:assert";
import { describe, it } from "node:test";
import bootsharp, { getDeclarations } from "../cs.mjs";

describe("export", () => {
    it("exports bootsharp api", () => {
        strictEqual(typeof bootsharp.boot, "function");
        strictEqual(typeof bootsharp.exit, "function");
        strictEqual(typeof bootsharp.resources, "object");
    });
    it("exports dotnet api", () => {
        strictEqual(typeof bootsharp.dotnet.builder, "object");
        strictEqual(typeof bootsharp.dotnet.native, "object");
        strictEqual(typeof bootsharp.dotnet.runtime, "object");
    });
    it("exports type declarations", () => {
        assert(getDeclarations());
    });
});
