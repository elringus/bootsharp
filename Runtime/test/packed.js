const assert = require("assert");
const dotnet = require("./project/bin/dotnet");

describe("packed library", () => {
    it("can boot without specifying boot data", async () => {
        await assert.doesNotReject(dotnet.boot);
        assert.deepStrictEqual(dotnet.getBootStatus(), dotnet.BootStatus.Booted);
    });
    it("can interop without specifying assembly", async () => {
        assert.deepStrictEqual(dotnet.invoke("JoinStrings", "a", "b"), "ab");
        assert.deepStrictEqual(await dotnet.invokeAsync("JoinStringsAsync", "a", "b"), "ab");
    });
    it("re-exports other dotnet functions", async () => {
        assert(dotnet.BootStatus);
        assert(dotnet.getBootStatus);
        assert(dotnet.boot);
        assert(dotnet.terminate);
        assert(dotnet.createObjectReference);
        assert(dotnet.disposeObjectReference);
        assert(dotnet.createStreamReference);
    });
});
