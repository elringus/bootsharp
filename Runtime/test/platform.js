const assert = require("assert");
const dotnet = require("../dist/dotnet");
const { bootTest } = require("./project");

const invoke = (name, ...args) => dotnet.invoke("Test", name, ...args);

describe("platform", () => {
    before(bootTest);
    after(dotnet.terminate);
    it("can provide guid", () => {
        const guid1 = invoke("GetGuid");
        const guid2 = invoke("GetGuid");
        assert.deepStrictEqual(guid1.length, 36);
        assert.deepStrictEqual(guid2.length, 36);
        assert.notDeepEqual(guid1, guid2);
    });
});
