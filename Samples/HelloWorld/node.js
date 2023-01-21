// Example on consuming the library as CommonJS module.
// Run 'node node.js' to test.

const dotnet = require("./Project/bin/dotnet");

// Providing implementation for 'GetHostName' function declared in 'HelloWorld' C# assembly.
dotnet.HelloWorld.getHostName = () => "Node.js";

(async function () {
    // Booting the DotNet runtime and invoking entry point.
    await dotnet.boot();
    // Invoking 'GetName()' C# method defined in 'HelloWorld' assembly.
    const guestName = dotnet.HelloWorld.getName();
    console.log(`Welcome, ${guestName}! Enjoy your CommonJS module space.`);
})();
