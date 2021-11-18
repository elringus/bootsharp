const vscode = require("vscode");
const dotnet = require("../../HelloWorld/Project/bin/HelloWorld");

module.exports = {
    activate,
    deactivate,
    getName
};

function activate(context) {
    const command = vscode.commands.registerCommand("dotnetjs.hello", async () => {
        // Booting the DotNet runtime and invoking entry point.
        await dotnet.boot();
        // Invoking 'GetName()' method from DotNet.
        const guestName = dotnet.invoke("GetName");
        const message = `Welcome, ${guestName}! Enjoy your VS Code extension space.`;
        vscode.window.showInformationMessage(message);
    });
    context.subscriptions.push(command);
}

function deactivate() {
    dotnet.terminate();
}

// This function is invoked by DotNet.
function getName() {
    return `${vscode.appName} ${vscode.appHost}`;
}
