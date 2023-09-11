// Named exports are auto-generated on C# build.
import bootsharp, { Backend, Frontend } from "./cs/bin/bootsharp/bootsharp.js";

// Binding 'IFrontend.GetName' endpoint invoked in C#.
Frontend.getName = () =>
    typeof Bun === "object" ? `Bun ${Bun.version}` :
    typeof Deno === "object" ? `Deno ${Deno.version.deno}` :
    typeof process === "object" ? `Node ${process.version}` :
    "Unknown JavaScript Runtime";
// Initializing dotnet runtime and invoking entry point.
await bootsharp.boot();
// Invoking 'IBackend.GetName' C# method.
console.log(`Hello ${Backend.getName()}!`);
