// Named exports are auto-generated on C# build.
import bootsharp, { Backend, Frontend } from "./cs/bin/bootsharp/bootsharp.js";

// Binding 'IFrontend.GetName' endpoint invoked in C#.
Frontend.getName = () => "Node.js";
// Initializing dotnet runtime and invoking entry point.
await bootsharp.boot();
// Invoking 'IBackend.GetName' C# method.
console.log(`Hello ${Backend.getName()}!`);

process.env.BOOTSHARP_E2E_MINIMAL = "PASS";
process.exit();
