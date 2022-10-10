// noinspection JSUnusedGlobalSymbols (assigned via node cli options)
export default class extends require("jest-environment-jsdom") {
    async setup() {
        await super.setup();
        // Define a flag to prevent dotnet worker init; required for tests to work.
        Object.defineProperty(this.global, "muteDotNetWorker", { value: true });
    }
}
