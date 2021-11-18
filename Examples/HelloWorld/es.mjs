// Example on consuming the library as ECMAScript module. 
// Run 'node es.mjs' with node v17 or later to test.

import HelloWorld from "./Project/bin/HelloWorld.js";

// This function is invoked by DotNet.
global.getName = () => `node ${process.version}`;

(async function () {
    // Booting the DotNet runtime and invoking entry point.
    await HelloWorld.boot();
    // Invoking 'GetName()' method from DotNet.
    const guestName = HelloWorld.invoke("GetName");
    console.log(`Welcome, ${guestName}! Enjoy your ES module space.`);
})();
