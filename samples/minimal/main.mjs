// Named exports are auto-generated on C# build.
import bootsharp, { Global } from "./cs/bin/bootsharp/index.mjs";

// Binding 'Program.GetFrontendName' endpoint invoked in C#.
Global.Program.getFrontendName = () =>
    typeof Bun === "object" ? `Bun ${Bun.version}` :
    typeof Deno === "object" ? `Deno ${Deno.version.deno}` :
    typeof process === "object" ? `Node ${process.version}` :
    "Unknown JavaScript Runtime";

// Subscribing to 'Program.OnMainInvoked' C# event.
Global.Program.onMainInvoked.subscribe(console.log);

// Initializing dotnet runtime and invoking entry point.
await bootsharp.boot();

// Invoking 'Program.GetBackendName' C# method.
console.log(`Hello ${Global.Program.getBackendName()}!`);
