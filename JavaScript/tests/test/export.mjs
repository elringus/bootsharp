import assert from "node:assert";
import { describe, it } from "node:test";
import bootsharp from "../cs.mjs";

describe("export", () => {
    it("exports bootsharp api", () => {
        assert.strictEqual(typeof bootsharp.boot, "function");
        assert.strictEqual(typeof bootsharp.exit, "function");
        assert.strictEqual(typeof bootsharp.resources, "object");
    });
    it("exports dotnet api", () => {
        assert.strictEqual(typeof bootsharp.dotnet.builder, "object");
        assert.strictEqual(typeof bootsharp.dotnet.native, "object");
        assert.strictEqual(typeof bootsharp.dotnet.runtime, "object");
    });
});
