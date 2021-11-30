import * as vscode from "vscode";
import * as dotnet from "../../HelloWorld/Project/bin/dotnet";

// Providing implementation for 'GetHostName' function declared in 'HelloWorld' C# assembly.
dotnet.HelloWorld.GetHostName = () => "VS Code";

export async function activate(context: vscode.ExtensionContext) {
    // Booting the DotNet runtime and invoking entry point.
    try { await dotnet.boot(); }
    catch (e: any) { vscode.window.showErrorMessage(e.message); }
    const command = vscode.commands.registerCommand("dotnetjs.hello", greet);
    context.subscriptions.push(command);
}

export async function deactivate() {
    await dotnet.terminate();
}

function greet() {
    // Invoking 'GetName()' C# method defined in 'HelloWorld' assembly.
    const guestName = dotnet.HelloWorld.GetName();
    const message = `Welcome, ${guestName}! Enjoy your VS Code extension space.`;
    vscode.window.showInformationMessage(message);
}
