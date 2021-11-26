const { boot, invoke, terminate } = require("./project/bin/packed");
const assert = require("assert");

describe("benchmark", () => {
    after(terminate);
    it("boot", boot);
    it("compute", () => invoke("Test", "ComputePrime", 52000));
    it("interop", () => {
        const cycles = 23000;
        const instance = invoke("Test", "CreateInstance");
        for (let i = 0; i <= cycles; i++)
            instance.invokeMethod("SetVar", i.toString());
        const performed = instance.invokeMethod("GetVar");
        assert.deepStrictEqual(performed, cycles.toString());
        instance.dispose();
    });
});
