const assert = require("assert");
const packed = require("./project/bin/packed");

describe("packed library", () => {
    after(packed.terminate);
    it("can boot without specifying boot data", async () => {
        await assert.doesNotReject(packed.boot);
        assert.deepStrictEqual(packed.getBootStatus(), packed.BootStatus.Booted);
    });
    it("can interop with specifying assembly", async () => {
        assert.deepStrictEqual(packed.invoke("Test", "JoinStrings", "a", "b"), "ab");
        assert.deepStrictEqual(await packed.invokeAsync("Test", "JoinStringsAsync", "a", "b"), "ab");
    });
    it("re-exports other dotnet functions", async () => {
        assert(packed.BootStatus instanceof Object);
        assert(packed.getBootStatus instanceof Function);
        assert(packed.terminate instanceof Function);
        assert(packed.createObjectReference instanceof Function);
        assert(packed.disposeObjectReference instanceof Function);
        assert(packed.createStreamReference instanceof Function);
    });
    it("can interop via generated methods", async () => {
        global.dotnetjs_packed_internal = {
            Generated: {
                EchoGenerated: value => value
            }
        };
        packed.Generated = dotnetjs_packed_internal.Generated;
        assert.deepStrictEqual(packed.Generated.EchoGenerated("a"), "a");
    });
});
