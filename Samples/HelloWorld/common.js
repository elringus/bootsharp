// Example on consuming the library as CommonJS module. 
// Run 'node common.js' to test.

const HelloWorld = require("./Project/bin/HelloWorld");

// This function is invoked by DotNet.
global.getName = () => `node ${process.version}`;

(async function () {
    // Booting the DotNet runtime and invoking entry point.
    await HelloWorld.boot();
    // Invoking 'GetName()' method from DotNet.
    const guestName = HelloWorld.invoke("GetName");
    console.log(`Welcome, ${guestName}! Enjoy your CommonJS module space.`);
})();
