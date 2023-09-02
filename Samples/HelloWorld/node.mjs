// Example on using bootsharp in node.
// Publish the dotnet backend and run 'node node.mjs' to test.

import bootsharp, { HelloWorld } from "./backend/bin/bootsharp/bootsharp.js";

// Binding 'GetHostName' method declared in 'HelloWorld' C# assembly.
HelloWorld.getHostName = () => "Node.js";
console.log(HelloWorld.getName())
// Initializing dotnet runtime and invoking entry point.
await bootsharp.boot();
// Invoking 'GetName()' C# method defined in 'HelloWorld' assembly.
// const name = HelloWorld.getName();
// console.log(`Hello ${name}!`);
