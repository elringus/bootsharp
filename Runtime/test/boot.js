const assert = require("assert");
const { boot, terminate, getBootStatus, BootStatus } = require("../dist/dotnet");
const { bootExample } = require("../example/example");

describe("boot", () => {
    it("is in standby by default", () => {
        assert.deepStrictEqual(getBootStatus(), BootStatus.Standby);
    });
    it("throws when no boot data provided", async () => {
        await assert.rejects(boot, { message: "Boot data is missing." });
    });
    it("throws when no assemblies provided", async () => {
        const data = { entryAssemblyName: "", assemblies: [] };
        await assert.rejects(boot(data), { message: "Boot assemblies are missing." });
    });
    it("throws when entry assembly is not found", async () => {
        const assembly = { name: "Foo.dll" };
        const data = { entryAssemblyName: "Bar.dll", assemblies: [assembly] };
        await assert.rejects(boot(data), { message: "Entry assembly is not found." });
    });
    it("throws when assembly data is not assigned", async () => {
        const assembly = { name: "Foo.dll" };
        const data = { entryAssemblyName: "Foo.dll", assemblies: [assembly] };
        await assert.rejects(boot(data), { message: "Foo.dll assembly data is invalid." });
    });
    it("throws when assembly data length is zero", async () => {
        const assembly = { name: "Foo.dll", data: new Uint8Array(0) };
        const data = { entryAssemblyName: "Foo.dll", assemblies: [assembly] };
        await assert.rejects(boot(data), { message: "Foo.dll assembly data is invalid." });
    });
    it("throws when attempting to boot while already booted", async () => {
        await bootExample();
        await assert.rejects(bootExample, { message: "Invalid boot status. Expected: Standby. Actual: Booted." });
        terminate();
    });
    it("throws when attempting to boot while booting", async () => {
        const promise = bootExample();
        await assert.rejects(bootExample, { message: "Invalid boot status. Expected: Standby. Actual: Booting." });
        await promise;
        terminate();
    });
    it("throws when attempting to terminate while not booted", () => {
        assert.throws(terminate, { message: "Invalid boot status. Expected: Booted. Actual: Standby." });
    });
    it("boots when in standby", async () => {
        await bootExample();
        assert.deepStrictEqual(getBootStatus(), BootStatus.Booted);
        terminate();
    });
    it("terminates when booted", async () => {
        await bootExample();
        terminate();
        assert.deepStrictEqual(getBootStatus(), BootStatus.Standby);
    });
});
