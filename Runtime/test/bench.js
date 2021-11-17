const { boot, invoke, terminate } = require("./project/bin/dotnet");

describe("benchmark", () => {
    after(terminate);
    it("boot", boot);
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
