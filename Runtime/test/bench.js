const dotnet = require("../dist/dotnet");
const { getBootData } = require("./project");

const invoke = (name, ...args) => dotnet.invoke("Test", name, ...args);
const bootData = getBootData();

describe("benchmark", () => {
    after(dotnet.terminate);
    it("boot", () => dotnet.boot(bootData));
    it("compute", () => invoke("ComputePrime", 50000));
    it("interop", () => {
        for (let i = 0; i < 5000; i++) {
            const instance = invoke("CreateInstance");
            instance.invokeMethod("SetVar", "foo");
            instance.invokeMethod("GetVar", "foo");
            instance.dispose();
        }
    });
});
