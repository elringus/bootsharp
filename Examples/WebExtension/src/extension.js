const vscode = require("vscode");
const dotnet = require("../../HelloWorld/Project/bin/HelloWorld");

module.exports = {
    activate: async context => {
        // Booting the DotNet runtime and invoking entry point.
        try { await dotnet.boot(); } catch (e) { vscode.window.showErrorMessage(e.message); }
        const command = vscode.commands.registerCommand("dotnetjs.hello", greet);
        context.subscriptions.push(command);
    },
    deactivate: dotnet.terminate
};

function greet() {
    // Invoking 'GetName()' method from DotNet.
    const guestName = dotnet.invoke("GetName");
    const message = `Welcome, ${guestName}! Enjoy your VS Code extension space.`;
    vscode.window.showInformationMessage(message);
}

// This function is invoked by DotNet.
global.getName = () => "VS Code";
