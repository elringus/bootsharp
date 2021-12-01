const assert = require("assert");
const dotnet = require("../dist/dotnet");
const bootData = require("./project").getBootData();

describe("benchmark", () => {
    after(dotnet.terminate);
    it("boot", () => dotnet.boot(bootData));
    it("compute", () => dotnet.invoke("Test.Project", "ComputePrime", 52000));
    it("interop", () => {
        const cycles = 23000;
        const instance = dotnet.invoke("Test.Project", "CreateInstance");
        for (let i = 0; i <= cycles; i++)
            instance.invokeMethod("SetVar", i.toString());
        const performed = instance.invokeMethod("GetVar");
        assert.deepStrictEqual(performed, cycles.toString());
        instance.dispose();
    });
});
